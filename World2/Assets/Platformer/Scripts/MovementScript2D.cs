
/*
    This script is a 2D movement script that can be used for any 2D game.
    It handles movement, jumping, gravity, and collisions with terrain.
    It is designed to be easy to use and modify to suit your needs.
    It is designed to be used on a 2D character with a box collider.
*/


using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;


[RequireComponent(typeof(BoxCollider2D))]
public class MovementScript2D : MonoBehaviour
{
    private const int BIG_NUMBER = 1000000;

    #region Variables

    private BoxCollider2D   _collider;
    private Vector3         _speed;
    private Vector3         _movement;
    private float           _caoyteTimer;
    private float           _jumpApexYPoint;
    private float           _apexSpeedModifier;
    
    [Space]
    [Header("Movement Variables")]
    [Space]
    [SerializeField]                    private float jumpHeight                = 2;
    [Range(0, 0.4f)][SerializeField]    private float jumpApexCorrectionSpeed   = 0.1f;
    [Range(0, 0.4f)][SerializeField]    private float jumpApexCorrectionDist    = 0.1f;
    [SerializeField]                    private float gravityValue              = 9.81f;
    [Range(0, 0.4f)][SerializeField]    private float stepSize                  = 0.2f;
    [Range(0, 0.4f)][SerializeField]    private float inclineDist               = 0.25f;
    [SerializeField]                    private float accel                     = 5;
    [SerializeField]                    private float deAccel                   = 10;
    [SerializeField]                    private float maxSpeed                  = 5;
    [SerializeField]                    private LayerMask terrainLayer          = default;
    [SerializeField]                    private float maxAngle                  = 45;
    [Range(0, 0.5f)][SerializeField]    private float cayoteTime                = 0.2f;
    [Range(0, 0.4f)][SerializeField]    private float jumpAgainDistance         = 0.1f;
    
    [Space]
    [Header("Raycast Variables")]
    [Space]
    [SerializeField] private int rayNum             = 5;
    private CollisionData _vertical, _horizontal;
    private InputData inputs;
    #endregion

    #region Properties
    public BoxCollider2D Collider => _collider;
    public Vector3 Speed => _speed;
    public Vector3 Movement => _movement;
    #endregion
    
    private void Start()
    {
        _collider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        //Get Inputs
        GetInputs();

        //Reset data 
        _movement = Vector3.zero;
        _vertical = new CollisionData();
        _horizontal = new CollisionData();
        
        //Perform static collision checks
        PreMovementCollisions();

        //Gravity
        if (_vertical.Col) _speed.y = 0;
        else _speed.y += gravityValue * Time.deltaTime;
        
        //Movement calculations
        CalculateMovement();

        //Perform jump apex correction
        JumpApexCorrection();

        //Perform collision checks after movement is calculated
        CheckCollisionsHorizontal();
        CheckCollisionsVertical();

        //Set the movement vector         
        _movement += Time.deltaTime * _speed;
        _movement.x *= 1 + _apexSpeedModifier;

        //Perform collision handling
        HandleCollisions();
        
        //Invert y component after calculations
        _movement.y = -_movement.y;

        //Move the player//Debug.Log
        transform.position += _movement;
    }

    private void HandleCollisions()
    {
        if (_horizontal.Col)
        {
            _jumpApexYPoint = BIG_NUMBER;
            _movement.x = (_horizontal.Hit.distance - _collider.bounds.extents.x) * Mathf.Sign(_horizontal.Hit.point.x - transform.position.x);
            _speed.x = 0;
        }

        if(_vertical.Col)
        {
            _jumpApexYPoint = BIG_NUMBER;
            _caoyteTimer = cayoteTime;
            _movement.y = (_vertical.Hit.distance - _collider.bounds.extents.y) * Mathf.Sign(transform.position.y - _vertical.Hit.point.y);
            _speed.y = 0;
            //Debug.Log("Vertical Collision");

            if(_vertical.AngleCol)
            {
                //if(_speed.x != 0)
               //{
                    _movement.x = Mathf.Cos(Vector2.Angle(Vector2.up, _vertical.Hit.normal) * Mathf.Deg2Rad) * _speed.x * Time.deltaTime;
                //}
            }
        }
        else
        {
            _caoyteTimer -= Time.deltaTime;
        }
    }

    private void JumpApexCorrection()
    {
        if(_jumpApexYPoint - transform.position.y < jumpApexCorrectionDist)
        {
            var distanceFromApex = _jumpApexYPoint - transform.position.y;
            _apexSpeedModifier = Mathf.Lerp(jumpApexCorrectionSpeed, 0, distanceFromApex / jumpApexCorrectionDist);
        }
        else
        {
            _apexSpeedModifier = 0;
        }
    }

    private void PreMovementCollisions()
    {
        _vertical.Col = _horizontal.Col = false;
        
        //vertical
        if (_speed.y == 0)
        {
            var playerPos = transform.position;
            RaycastHit2D[] hits;
            
            //Grounded Check
            hits = PerformRayCast(new Vector3(_collider.bounds.extents.x - 0.05f, 0), Vector2.down, stepSize + _collider.bounds.extents.y, Color.red);
            
            _vertical.AngleCol = hits.Any(x => Vector2.Angle(Vector2.up * Mathf.Sign(_speed.y), x.normal) > 0);
            //if ground has an angle
            if (_vertical.AngleCol)
            {
                hits = PerformRayCast(new Vector3(_collider.bounds.extents.x - 0.05f - inclineDist, 0), Vector2.down, stepSize + _collider.bounds.extents.y, Color.green);
            }

            if (hits.Any(x => x.distance != 0))
            {    
                var ray = hits.Where(x => x.distance != 0).Aggregate((x, y) => x.distance < y.distance ? x : y);
                //Check if the angle is too steep
                if (Vector2.Angle(Vector2.up, ray.normal) < maxAngle)
                {
                    _vertical.Col = true;
                    _vertical.Hit = ray;
                }
            }
        }

        //horizontal
        if(_speed.x == 0)
        {
            var playerPos = transform.position;
            RaycastHit2D[] hits;
            
            //right
            hits = PerformRayCast(new Vector3(0, _collider.bounds.extents.y - stepSize), Vector2.right, _collider.bounds.extents.x, Color.blue);
            
            //Check if theres a collision
            if(hits.Any(x => x.distance != 0 && x.distance < 0.5f))
            {
                //Query the closest hit
                var ray = hits.Where(x => x.distance != 0).Aggregate((x, y) => x.distance < y.distance ? x : y);
                _horizontal.Col = true;
                _horizontal.Hit = ray;
                
            }

            //left
            hits = PerformRayCast(new Vector3(0, _collider.bounds.extents.y - stepSize), Vector2.left, 0.5f, Color.blue);
            
            //Check if theres a collision
            if(hits.Any(x => x.distance != 0 && x.distance < 0.5f))
            {
                //Query the closest hit
                var ray = hits.Where(x => x.distance != 0).Aggregate((x, y) => x.distance < y.distance ? x : y);
                _horizontal.Col = true;
                _horizontal.Hit = ray;
                
            }
        }
    }

    private void CalculateMovement()
    {
        if (inputs.input.x == 0 || inputs.input.x == Math.Sign(-_speed.x))
        {
            if(inputs.input.x == Math.Sign(-_speed.x))
                _speed.x = Mathf.MoveTowards(_speed.x, 0, deAccel * 2 < accel ? deAccel * 2 * Time.deltaTime : accel * Time.deltaTime);
            else
                _speed.x = Mathf.MoveTowards(_speed.x, 0, deAccel * Time.deltaTime);
            
        }
        else
        {
            _speed.x += inputs.input.x * Time.deltaTime * accel;
            _speed.x = Mathf.Clamp(_speed.x, -maxSpeed, maxSpeed);
        }
        

        if (inputs.jump)
        {
            if (_vertical.Col || _caoyteTimer > 0)
            {
                _caoyteTimer = 0;
                //Calculate jump height in units based on gravity and jump speed
                _speed.y = -math.sqrt(2 * gravityValue * jumpHeight);
                //calculate the jump apex y distance
                _jumpApexYPoint = _speed.y * _speed.y / (2 * gravityValue) + transform.position.y;
                return;
            }
            
            var hits = PerformRayCast(new Vector3(_collider.bounds.extents.x - 0.05f, 0), Vector2.down, jumpAgainDistance + _collider.bounds.extents.y, Color.yellow);
            if(hits.Any(x => x.distance != 0) && !hits.Any(x => Vector2.Angle(Vector2.up, x.normal) > maxAngle))
            {
                //Calculate jump height in units based on gravity and jump speed
                _speed.y = -math.sqrt(2 * gravityValue * jumpHeight);
                //calculate the jump apex y distance
                _jumpApexYPoint = _speed.y * _speed.y / (2 * gravityValue) + transform.position.y;
                return;
            }
        }
    }

    private void CheckCollisionsHorizontal()
    {
        //horizontal
        if (_speed.x != 0)
        {
            var playerPos = transform.position;
            RaycastHit2D[] hits;

            hits = PerformRayCast(new Vector3(0, _collider.bounds.extents.y - stepSize), Vector2.right * Mathf.Sign(_speed.x), Mathf.Abs(_movement.x) + _collider.bounds.extents.x, Color.red);

            //Check if theres a collision
            if(hits.Any(x => x.distance != 0))
            {
                //Query the closest hit
                var ray = hits.Where(x => x.distance != 0).Aggregate((x, y) => x.distance < y.distance ? x : y);
                //Check if the angle is too steep
                if (Vector2.Angle(Vector2.up, ray.normal) > maxAngle)
                {
                    _horizontal.Col = true;
                    _horizontal.Hit = ray;
                }
            }
        }
    }

    private void CheckCollisionsVertical()
    {
        if(_speed.y != 0 )
        {
            _vertical.Col = false;

            var playerPos = transform.position;
            RaycastHit2D[] hits;
            
            hits = PerformRayCast(new Vector3(_collider.bounds.extents.x - 0.05f, 0), Vector2.down * Mathf.Sign(_speed.y), Time.deltaTime * Mathf.Abs(_speed.y) + _collider.bounds.extents.y, Color.red);

            _vertical.AngleCol = hits.Any(x => Vector2.Angle(Vector2.up * Mathf.Sign(_speed.y), x.normal) > 0);
            //if ground has an angle
            if (_vertical.AngleCol)
            {
                
                hits = PerformRayCast(new Vector3(_collider.bounds.extents.x - 0.05f - inclineDist, 0), Vector2.down * Mathf.Sign(_speed.y), Time.deltaTime * Mathf.Abs(_speed.y) + _collider.bounds.extents.y, Color.green);
            }

            
            if (hits.Any(x => x.distance != 0))
            {
                var ray = hits.Where(x => x.distance != 0).Aggregate((x, y) => x.distance < y.distance ? x : y);
                //Check if the angle is too steep
                if (Vector2.Angle(Vector2.up * Mathf.Sign(Speed.y), ray.normal) < maxAngle)
                {
                    _vertical.Col = true;
                    _vertical.Hit = ray;
                }
            }
        }
    }

    private RaycastHit2D[] PerformRayCast(Vector3 oriOffset, Vector2 dir, float dist, Color debugColor = default)
    {
        var playerPos = transform.position;
        var hits = new RaycastHit2D[rayNum];
        for (int i = 0; i < rayNum; i++)
        {
            var rayOri = Vector3.Lerp(playerPos + (oriOffset.y == 0 ? oriOffset : new Vector3(0, _collider.bounds.extents.y - 0.05f)),
                playerPos - oriOffset, i / (float)(rayNum - 1));
            Debug.DrawRay(rayOri, dir * dist, debugColor);
    
            hits[i] = Physics2D.Raycast(rayOri, dir, dist, terrainLayer);
        }
        return hits;
    }

    private void GetInputs()
    {
        inputs.input = Vector2.zero;
        inputs.input.x += Keyboard.current.dKey.isPressed ? 1 : 0;
        inputs.input.x -= Keyboard.current.aKey.isPressed ? 1 : 0;

        inputs.jump = Keyboard.current.spaceKey.isPressed;
    }
    private struct CollisionData
    {
        public bool Col;
        public RaycastHit2D Hit;
        public bool AngleCol;
    }
    private struct InputData
    {
        public Vector2 input;
        public bool jump;
    }

}

