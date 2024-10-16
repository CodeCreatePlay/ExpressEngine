using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpressEngine.Game
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterMotor : MonoBehaviour, ICharacterMotor
    {
        public const float kMaxSpeedChange = 300f;

        public enum VelocityPhysicsMode
        {
            Raw,
            Simple,
        }

        #region Exposed Fields

        [Header("Collider Configuration")]
        [SerializeField][Min(0f)] private float _height = 2f;
        [SerializeField][Min(0f)] private float _thickness = 1f;
        [SerializeField] private Vector3 _colliderOffset = Vector3.zero;

        [Header("Velocity Physics")]
        [SerializeField] private VelocityPhysicsMode _velocityMode = VelocityPhysicsMode.Simple;
        [SerializeField][Range(0f, kMaxSpeedChange)] private float _speedChangeRate = 50f;
        [SerializeField][Range(0f, 1f)] private float _airControl = 0f;
        [SerializeField] private bool _enableGravity = true;
        [SerializeField][Min(0f)] private float _gravityAccel = 20f;
        [SerializeField][Min(0f)] private float _maxFallSpeed = 20f;

        [Header("Ground Detection")]
        [Tooltip("Detect ground in these layers.")]
        [SerializeField] private LayerMask _groundMask = 1 << 0;

        [Tooltip("If true, snaps to ground when grounded. Useful for staying on the ground when moving down slopes.")]
        [SerializeField] private bool _shouldSnapToGround = true;

        [Tooltip("Surfaces with plane normal angle below this angle is considered as ground.")]
        [SerializeField][Range(0f, 90f)] private float _groundAngleLimit = 90f;

        [Tooltip("Surfaces with plane normal angle below this angle is considered as flat ground.")]
        [SerializeField][Range(0f, 90f)] private float _flatGroundAngleLimit = 60f;

        [SerializeField][Min(0f)] private float _groundProbeDistance = 10f;
        [SerializeField][Min(0f)] private float _groundProbeThickness = 0.1f;

        [Tooltip("Minimum extra ground threshold distance for snapping to ground. The greater value between step height and this value will be used.")]
        [SerializeField][Min(0f)] private float _minExtraGroundThreshold = 0.25f;

        [Tooltip("Factor to multiply by desired ground distance to account for floating point errors.")]
        [SerializeField][Min(0)] private float _groundCheckToleranceFactor = 0.01f;

        [Tooltip("If true, performs predictive ground probing and stops horizontal velocity if the final velocity will cause the character to leave ground.")]
        [SerializeField] private bool _restrictToGround = false;

        [Header("Step Handling")]
        [SerializeField][Min(0f)] private float _stepHeight = 0.3f;
        [SerializeField][Min(1f)] private float _stepUpSmooth = 1f;
        [SerializeField][Min(1f)] private float _stepDownSmooth = 1f;
        [SerializeField] private bool _useRealGroundNormal = true;

        [Tooltip("If true, performs predictive ground probing to adjust floating step velocity more responsively.")]
        [SerializeField] private bool _predictGroundWhenFalling = true;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showVelocityDebug = false;
        [SerializeField] private bool _showGroundProbingDebug = false;
        [SerializeField] private bool _showSlopeDebug = false;
        [SerializeField] private bool _showContactDebug = false;
#endif

        #endregion

        #region Cache Fields

        // Convenience cache for collider.
        private CapsuleCollider _capsuleCollider;
        private float _capsuleHalfHeight;
        // Points with ground normal dot larger than this value is considered as ground.
        private float _minGroundAngleDot;
        // Points with ground normal dot larger than this value is considered as flat ground.
        private float _minFlatGroundAngleDot;

        // Ongoing collisions. Should be cleared at the end of each fixed update.
        private readonly List<Collision> _collisions = new();

        #endregion

        #region Velocity Fields

        // The target active velocity to aim for.
        private Vector3 _activeVelocityTarget = Vector3.zero;
        // The current active velocity, affected by velocity physics.
        private Vector3 _activeVelocity = Vector3.zero;

        // Accumulated gravity speed;
        private float _gravitySpeed = 0f;

        // Velocity contribution from connected body (i.e. floor).
        private Vector3 _connectedBodyVel = Vector3.zero;
        private Vector3 _connectedBodyPosDelta = Vector3.zero;

        // Ground step height adjustment velocity;
        private Vector3 _groundStepVel = Vector3.zero;

        #endregion

        #region Ground Fields

        // -- Ground state.
        private bool _wasOnGround = false;
        private bool _hasGroundStateChanged = false;
        private bool _isTouchingCeiling = false;

        // -- Ground contact.
        private Vector3 _prevConnectionPos;

        // -- Ground probing.
        private float _groundThresholdDistance;
        private bool _useExtraGroundThresholdDistance = false;
        // Extra ground threshold distance for maintaining grounded when going down stairs or slopes.
        private float _extraGroundThresholdDistance;
        // Desired ground distance from collider center.
        private float _desiredGroundDistance;
        // Ground probe range.
        private float _totalGroundProbeDistance;

        #endregion

        #region Properties

        // Data.
        public bool IsOnGround { get; set; }
        public bool IsOnFlatGround { get; set; }
        public Vector3 GroundNormal { get; set; }
        public Vector3 GroundPoint { get; set; }
        public Collider GroundCollider { get; set; }
        public Vector3 SlopeNormal { get; set; }
        public Vector3 Velocity { get => Rigidbody.velocity; }
        public float Speed => Rigidbody.velocity.magnitude;
        public Vector3 Direction => Rigidbody.velocity.normalized;

        // Components.
        public Rigidbody Rigidbody { get; private set; }
        public Collider Collider { get; private set; }
        public Vector3 ColliderCenter { get => _capsuleCollider.bounds.center; }

        // Configuration.
        /// <summary>
        /// Capsule collider height.
        /// </summary>
        public float Height { get => _height; set { _height = value; UpdateColliderHeight(); } }
        /// <summary>
        /// Capsule collider thickness is equal to 2 times its radius.
        /// </summary>
        public float Thickness { get => _thickness; set { _thickness = value; UpdateColliderRadius(); } }
        public float SpeedChangeRate { get => _speedChangeRate; set => _speedChangeRate = value; }
        public float AirControl { get => _airControl; set => _airControl = value; }
        public bool ShouldSnapToGround { get => _shouldSnapToGround; set => _shouldSnapToGround = value; }
        /// <summary>
        /// Ground angle within this angle limit is considered ground.
        /// </summary>
        public float GroundAngleLimit {
            get => _groundAngleLimit;
            set {
                _groundAngleLimit = value;
                _minGroundAngleDot = Mathf.Cos(value * Mathf.Deg2Rad);
            }
        }
        /// <summary>
        /// Ground angle within this angle limit is considered flat ground.
        /// </summary>
        public float FlatGroundAngleLimit {
            get => _flatGroundAngleLimit;
            set {
                _flatGroundAngleLimit = value;
                _minFlatGroundAngleDot = Mathf.Cos(value * Mathf.Deg2Rad);
            }
        }
        public float GroundProbeDistance { get => _groundProbeDistance; set => _groundProbeDistance = value; }
        /// <summary>
        /// If true, extend desired ground distance by step height to snap to ground when moving downstairs or slopes.
        /// </summary>
        public bool UseExtraGroundThresholdDistance {
            get => _useExtraGroundThresholdDistance;
            set {
                _useExtraGroundThresholdDistance = value;
                UpdateGroundSensorThresholdDistance();
            }
        }
        /// <summary>
        /// Extra ground threshold distance used to help snap to ground when moving downstairs or slopes.
        /// </summary>
        public float ExtraGroundThresholdDistance {
            get => _extraGroundThresholdDistance;
            set {
                _extraGroundThresholdDistance = value;
                UpdateGroundSensorThresholdDistance();
            }
        }
        /// <summary>
        /// Desired ground distance to maintain based on step height.
        /// </summary>
        public float DesiredGroundDistance {
            get => _desiredGroundDistance;
            set {
                _desiredGroundDistance = value;
                UpdateGroundSensorThresholdDistance();
            }
        }

        #endregion

        #region Events

        public event Action GainedGroundContact;
        public event Action LostGroundContact;

        #endregion

        #region MonoBehaviour

        private void OnValidate()
        {
            ConfigureComponents();
            UpdateColliderDimensions();
            _minFlatGroundAngleDot = Mathf.Cos(_flatGroundAngleLimit * Mathf.Deg2Rad);
        }

        private void Awake()
        {
            OnValidate();
            UseExtraGroundThresholdDistance = true;
            GroundNormal = SlopeNormal = Vector3.up;
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            DrawContactNormals();
#endif

            // Update collisions and detect ground.
            UpdateCollisions();
			
            // Update ground state and invoke events.
            UpdateGroundState(IsOnGround);
			
            // Update movement.
            UpdateMovement(Time.deltaTime);

            // Clean up frame.
            _collisions.Clear();
        }

        private void OnCollisionEnter(Collision collision)
        {
            _collisions.Add(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            _collisions.Add(collision);
        }

        #endregion

        #region Movement Execution Methods

        // Internal method for moving. Sets rigidbody velocity.
        private void InternalMove(Vector3 velocity)
        {
            if (ShouldSnapToGround) Rigidbody.velocity = velocity + _groundStepVel;
            else Rigidbody.velocity = velocity;
        }

        /// <summary>
        /// Directly move by the specified delta position.
        /// </summary>
        /// <param name="deltaPosition"></param>
        /// <param name="alignToGround"></param>
        /// <param name="restictToGround"></param>
        public void MoveDeltaPosition(Vector3 deltaPosition, bool alignToGround = true, bool restictToGround = false)
        {
            if (alignToGround)
            {
                deltaPosition = IsOnGround ? AlignVelocityToNormal(deltaPosition, SlopeNormal) : deltaPosition;
            }

            if (restictToGround)
            {
                GroundInfo predictedGroundInfo;
                bool willBeOnGround = ProbeGround(out predictedGroundInfo, deltaPosition);
                if (!willBeOnGround) return;
            }

            Rigidbody.MovePosition(Rigidbody.position + deltaPosition);
        }

        /// <summary>
        /// Move by setting the active velocity target using the specified input speed and direction.
        /// <para>Active velocity undergoes velocity physics calculations
        /// to reach the active velocity target.</para>
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="direction"></param>
        public void Move(float speed, Vector3 direction)
        {
            Move(speed * direction);
        }

        /// <summary>
        /// Move by setting the active velocity target.
        /// <para>Active velocity undergoes velocity physics calculations
        /// to reach the active velocity target.</para>
        /// </summary>
        /// <param name="velocity"></param>
        public void Move(Vector3 velocity)
        {
            _activeVelocityTarget = velocity;
        }

        /// <summary>
        /// Set the active velocity the active velocity target.
        /// <para>Active velocity will persist across fixed updates,
        /// and undergoes velocity physics calculations.</para>
        /// </summary>
        /// <param name="velocity"></param>
        public void SetActiveVelocity(Vector3 velocity)
        {
            _activeVelocity = velocity;
            _activeVelocityTarget = velocity;
        }

        public void ClearActiveVelocity()
        {
            _activeVelocity = Vector3.zero;
        }

        #endregion

        #region Movement Computation Methods

        private void UpdateMovement(float deltaTime)
        {
            // Calculate final velocity.
            Vector3 finalVel = UpdateFinalVelocity(deltaTime);

            if (!IsOnGround)
            {
                // Update buit-in gravity.
                _gravitySpeed += deltaTime * _gravityAccel;
                if (_gravitySpeed > _maxFallSpeed) _gravitySpeed = _maxFallSpeed;
                if (_enableGravity) finalVel.y -= _gravitySpeed;

                // Falling ground detection lag compenstation.
                if (_predictGroundWhenFalling && finalVel.y < 0f)
                {
                    bool willBeOnGround = ProbeGround(out GroundInfo predictedGroundInfo, finalVel * deltaTime);
                    if (willBeOnGround)
                        _groundStepVel = CalculateGroundStepVelocity(predictedGroundInfo.Distance, deltaTime, noSmoothing: true);
                }
            }
            else if (_restrictToGround)
            {
                GroundInfo predictedGroundInfo;
                bool willBeOnGround = ProbeGround(out predictedGroundInfo, finalVel * deltaTime);
                if (!willBeOnGround) finalVel.x = finalVel.z = 0f;
            }

            InternalMove(finalVel);
        }

        private Vector3 UpdateFinalVelocity(float deltaTime)
        {
            // Refresh active velocity. Apply velocity physics.
            _activeVelocity = UpdateVelocityPhysics(_activeVelocity, _activeVelocityTarget, deltaTime);

            Vector3 finalVelocity = Vector3.zero;
            SlopeNormal = CalculateSlopeNormal(GroundNormal);

            // Rotate velocity to align to ground.
            Vector3 groundAlignedVelocity = _activeVelocity;
            if (IsOnGround) groundAlignedVelocity = AlignVelocityToNormal(_activeVelocity, SlopeNormal);

            finalVelocity = groundAlignedVelocity;
            finalVelocity += _connectedBodyVel;

#if UNITY_EDITOR
            // Slope normal.
            if (_showVelocityDebug) Debug.DrawLine(transform.position, transform.position + SlopeNormal, Color.cyan);
            // Desired velocity line.
            if (_showVelocityDebug) Debug.DrawLine(transform.position, transform.position + finalVelocity, Color.yellow);
#endif

            return finalVelocity;
        }

        private Vector3 UpdateVelocityPhysics(Vector3 currentVelocity, Vector3 targetVelocity, float deltaTime)
        {
            // Add any velocity from input, apply velocity physics to existing active velocity.

            bool isNotActive = (currentVelocity == Vector3.zero) && (targetVelocity == Vector3.zero);
            if (!isNotActive)
            {
                switch (_velocityMode)
                {
                    case VelocityPhysicsMode.Raw:
                        currentVelocity = targetVelocity;
                        break;
                    case VelocityPhysicsMode.Simple:
                        currentVelocity = UpdateSmoothedVelocity(currentVelocity, targetVelocity, deltaTime,
                            _speedChangeRate, IsOnGround ? 1f : _airControl);
                        break;
                }
            }
            return currentVelocity;
        }

        private Vector3 UpdateSmoothedVelocity(Vector3 currentVelocity, Vector3 targetVelocity, float deltaTime,
            float accel = kMaxSpeedChange, float controlRatio = 1f)
        {
            Vector3 finalVel = currentVelocity;
            float speedChange = accel * controlRatio * deltaTime;
            finalVel = Vector3.MoveTowards(finalVel, targetVelocity, speedChange);
            return finalVel;
        }

        /// <summary>
        /// Detect and calculate an approximated slope normal.
        /// </summary>
        /// <param name="groundNormal"></param>
        /// <returns></returns>
        private Vector3 CalculateSlopeNormal(Vector3 groundNormal)
        {
            Vector3 slopeNormal = groundNormal;
            /*
            if (IsOnGround && _useSlopeProbing)
            {
                _ = (_slopeProbeFrontCount == 1 && _slopeProbeBackCount == 1) ?
                    ProbeSlope(out slopeNormal, basePoint: GroundPoint, offsetDirection: _lastNonZeroActiveDirection,
                        originY: ColliderCenter.y + _capsuleHalfHeight, range: _groundThresholdDistance + _capsuleHalfHeight,
                        _slopeProbeFrontOffset, _slopeProbeBackOffset)
                    :
                    ProbeSlopeArray(out slopeNormal, basePoint: GroundPoint, offsetDirection: _lastNonZeroActiveDirection,
                        originY: ColliderCenter.y + _capsuleHalfHeight, range: _groundThresholdDistance + _capsuleHalfHeight,
                        _slopeProbeFrontOffset, _slopeProbeFrontCount, _slopeProbeBackOffset, _slopeProbeBackCount);
            }
            */
            return slopeNormal;
        }

        /// <summary>
        /// Calculate the adjustment floating velocity needed to maintain desired ground step height.
        /// </summary>
        /// <param name="groundDistance"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        private Vector3 CalculateGroundStepVelocity(float groundDistance, float deltaTime,
            float offsetY = 0f, bool noSmoothing = false)
        {
            float requiredDelta = (_capsuleHalfHeight + _stepHeight + offsetY) - groundDistance;
            bool shouldGoUp = requiredDelta > 0f;

            Vector3 vel;
            if (_hasGroundStateChanged || _isTouchingCeiling || noSmoothing)
            {
                vel = Vector3.up * (requiredDelta / deltaTime);
            }
            else
            {
                float stepSmooth = shouldGoUp ? _stepUpSmooth : _stepDownSmooth;
                vel = Vector3.up * (requiredDelta / (deltaTime * stepSmooth));
            }
            return vel;
        }

        #endregion

        #region Ground Detection

        #region Collisions

        private void UpdateCollisions()
        {
            // Evaluate collisions.
            _isTouchingCeiling = false;
            _ = EvaluateContacts(out _, out _isTouchingCeiling, out _, out _, _collisions);

            // Init frame ground info.
            IsOnGround = false;
            GroundNormal = Vector3.up;
            GroundPoint = transform.position;
            _groundStepVel = Vector3.zero;

            // Perform ground probing and update floating adjustment velocity.
            IsOnGround = EvaluateProbeGround(out _);
        }

        /// <summary>
        /// Iterate through every collision contact point and perform checks.
        /// </summary>
        private bool EvaluateContacts(out bool isTouchingGround, out bool isTouchingCeiling,
            out Vector3 groundNormal, out float groundHeight, List<Collision> collisions)
        {
            bool hasContact = false;
            isTouchingGround = false;
            isTouchingCeiling = false;

            int groundCollisionCount = 0;
            Vector3 accGroundNormal = Vector3.zero;
            groundHeight = 0f;

            // For each collision.
            for (int i = 0; i < collisions.Count; i++)
            {
                Collision collision = collisions[i];
                bool isGroundCollision = false;
                int groundContactCount = 0;
                Vector3 accGroundContactsNormal = Vector3.zero; // Average ground normal from a collision.

                // For each contact in a collision.
                for (int j = 0; j < collision.contactCount; j++)
                {
                    ContactPoint contact = collision.GetContact(j);
                    hasContact = true;

                    // If is ground contact.
                    if (contact.normal.y > _minGroundAngleDot + 0.001f)
                    {
                        isTouchingGround = true;
                        isGroundCollision = true;
                        accGroundContactsNormal = contact.normal;
                        groundContactCount++;
                        // Update average ground height.
                        groundHeight = groundHeight * (groundContactCount - 1) / groundContactCount
                            + (contact.point.y / groundContactCount);
                    }
                    // If is ceiling contact.
                    else if (contact.normal.y < -0.001f)
                    {
                        isTouchingCeiling = true;
                    }
                }
                // If this is a ground collision.
                if (isGroundCollision)
                {
                    // Accumulate ground normal from this collision.
                    accGroundNormal += (accGroundContactsNormal / (float)groundContactCount);
                    groundCollisionCount++;
                }
            }
            // Average ground normal from all ground collisions.
            groundNormal = isTouchingGround ? accGroundNormal / (float)groundCollisionCount : Vector3.up;

            return hasContact;
        }

        #endregion

        #region Ground Probing

        /// <summary>
        /// Perform ground probing, and update ground floating adjustment velocity.
        /// </summary>
        /// <param name="groundInfo"></param>
        /// <returns></returns>
        private bool EvaluateProbeGround(out GroundInfo groundInfo)
        {
            return EvaluateProbeGround(out groundInfo, Vector3.zero);
        }

        /// <summary>
        /// Perform ground probing at an offset, and update ground floating adjustment velocity.
        /// </summary>
        /// <param name="groundInfo"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private bool EvaluateProbeGround(out GroundInfo groundInfo, Vector3 offset)
        {
            bool isOnGround = ProbeGround(out groundInfo, offset, _useRealGroundNormal);
            if (isOnGround)
            {
                GroundNormal = groundInfo.Normal;
                GroundPoint = groundInfo.Point;
                // Update ground connection velocity.
                if (groundInfo.Collider != GroundCollider)
                {
                    // This is a new connection, initialize the connection position and velocity.
                    _prevConnectionPos = groundInfo.Collider.transform.position;
                    _connectedBodyVel = Vector3.zero;
                    GroundCollider = groundInfo.Collider;
                }
                else
                {
                    Vector3 groundPos = GroundCollider.transform.position;
                    if (_wasOnGround)
                    {
                        _connectedBodyPosDelta = groundPos - _prevConnectionPos;
                        _connectedBodyVel = _connectedBodyPosDelta / Time.deltaTime;
                    }
                    _prevConnectionPos = groundPos;
                }
                // Update ground floating adjustment velocity.
                _groundStepVel = CalculateGroundStepVelocity(groundInfo.Distance, Time.deltaTime, _connectedBodyPosDelta.y);
            }

#if UNITY_EDITOR
            if (_showGroundProbingDebug)
            {
                Vector3 desiredGroundPoint = ColliderCenter + offset - new Vector3(0f, DesiredGroundDistance, 0f);
                // Desired ground distance.
                if (_showGroundProbingDebug) Debug.DrawLine(ColliderCenter + offset, desiredGroundPoint, Color.green);
            }
#endif

            return isOnGround;
        }

        /// <summary>
        /// Perform ground probing at collider center.
        /// </summary>
        /// <param name="groundInfo"></param>
        /// <returns></returns>
        public bool ProbeGround(out GroundInfo groundInfo, bool useRealGroundNormal = false)
        {
            return ProbeGround(out groundInfo, Vector3.zero, useRealGroundNormal);
        }

        /// <summary>
        /// Perform ground probing at an offset from collider center. 
        /// </summary>
        /// <param name="groundInfo"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public bool ProbeGround(out GroundInfo groundInfo, Vector3 offset, bool useRealGroundNormal = false)
        {
            // Ground probing.
            bool isOnGround = GroundSensor.ProbeGround(out groundInfo, _groundThresholdDistance,
                distance: _totalGroundProbeDistance, thickness: _groundProbeThickness,
                origin: ColliderCenter + offset, layerMask: _groundMask, useRealGroundNormal: useRealGroundNormal);
            return isOnGround;
        }

        #endregion

        #region Ground State

        private void UpdateGroundState(bool isOnGround)
        {
            // Gained ground contact.
            if (!_wasOnGround && IsOnGround)
            {
                _gravitySpeed = 0f;
                _hasGroundStateChanged = true;
                GainedGroundContact?.Invoke();
                UseExtraGroundThresholdDistance = true;
            }
            // Lost ground contact.
            else if (_wasOnGround && !IsOnGround)
            {
                _hasGroundStateChanged = true;
                LostGroundContact?.Invoke();
                UseExtraGroundThresholdDistance = false;
            }
            else
            {
                _hasGroundStateChanged = false;
            }
            _wasOnGround = IsOnGround;
        }

        private void UpdateGroundSensorThresholdDistance()
        {
            _groundThresholdDistance = _useExtraGroundThresholdDistance ?
                DesiredGroundDistance + ExtraGroundThresholdDistance
                : DesiredGroundDistance;
        }

        #endregion

        #endregion

        #region Helpers

        private void ConfigureComponents()
        {
            TryGetComponent(out Rigidbody rb);
            Rigidbody = rb;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.freezeRotation = true;

            TryGetComponent(out CapsuleCollider capsuleCollider);
            Collider = capsuleCollider;
            _capsuleCollider = (CapsuleCollider)Collider;
        }

        /// <summary>
        /// Align velocity to a plane defined by the specified plane normal.
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        private Vector3 AlignVelocityToNormal(Vector3 velocity, Vector3 normal)
        {
            float speed = velocity.magnitude;
            Vector3 alignedDirection = Quaternion.FromToRotation(Vector3.up, normal) * (velocity / speed);
            return speed * alignedDirection.normalized;
        }

        private void UpdateColliderDimensions()
        {
            UpdateColliderHeight();
            UpdateColliderRadius();
        }

        private void UpdateColliderHeight()
        {
            if (_stepHeight > _height) _stepHeight = _height;
			
            Vector3 center = _colliderOffset + new Vector3(0f, _height / 2f, 0f);
            center.y += _stepHeight / 2f;
			
            _capsuleCollider.center = center;
            _capsuleCollider.height = _height - _stepHeight;
			
            _capsuleHalfHeight = _capsuleCollider.height / 2f;
			
            LimitColliderRadius();
            UpdateGroundCheckDimensions();
        }

        public void UpdateColliderRadius()
        {
            float radius = _thickness / 2f;
            _capsuleCollider.radius = radius;

            LimitColliderRadius();
        }

        protected void UpdateGroundCheckDimensions()
        {
            // Update desired ground distance.
            DesiredGroundDistance = (_capsuleHalfHeight + _stepHeight) * (1 + _groundCheckToleranceFactor);
			
            // Update extra ground threshold distance used to snap to ground when going down stairs or slopes.
            ExtraGroundThresholdDistance = Mathf.Max(_stepHeight, _minExtraGroundThreshold);
			
            // Update total length for ground probing.
            _totalGroundProbeDistance = DesiredGroundDistance + _groundProbeDistance;
        }

        /// <summary>
        /// Restrict collider minimum thickness to collider height.
        /// </summary>
        private void LimitColliderRadius()
        {
            if (_capsuleCollider.radius * 2f > _capsuleCollider.height) _capsuleCollider.radius = _capsuleCollider.height / 2f;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void DrawContactNormals()
        {
            if (!_showContactDebug) return;
            for (int i = 0; i < _collisions.Count; i++)
            {
                Collision collision = _collisions[i];
                for (int j = 0; j < collision.contactCount; j++)
                {
                    ContactPoint contact = collision.GetContact(j);
                    Color color = Color.grey;
                    if (contact.normal.y > _minFlatGroundAngleDot)
                    {
                        color = Color.white;
                    }
                    Debug.DrawLine(contact.point, contact.point + contact.normal * 0.1f, color);
                }
            }
        }
#endif

        #endregion
    }
}