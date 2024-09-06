using BulletSharp;
using MMDMC.MMD;
using System.Numerics;

namespace MMDMC.Physics
{
    public class PhysicsScene
    {
        DefaultCollisionConfiguration defaultCollisionConfiguration = new DefaultCollisionConfiguration();
        DbvtBroadphase broadphase = new DbvtBroadphase();
        SequentialImpulseConstraintSolver sequentialImpulseConstraintSolver = new SequentialImpulseConstraintSolver();
        Dispatcher dispatcher;
        DiscreteDynamicsWorld world;
        public void Initialize()
        {
            dispatcher = new CollisionDispatcher(defaultCollisionConfiguration);
            world = new DiscreteDynamicsWorld(dispatcher, broadphase, sequentialImpulseConstraintSolver, defaultCollisionConfiguration);
            BulletSharp.Math.Vector3 gravity = new BulletSharp.Math.Vector3(0, -9.81f, 0);
            world.SetGravity(ref gravity);
        }
        public void SetGravitation(Vector3 g)
        {
            BulletSharp.Math.Vector3 gravity = GetVector3(g);
            world.SetGravity(ref gravity);
        }
        public void AddRigidBody(PhysicsRigidBody rb, PMX_RigidBody desc)
        {
            MotionState motionState;
            rb.defaultPosition = desc.Position;
            rb.defaultRotation = ToQuaternion(desc.Rotation);
            Matrix4x4 mat = Matrix4x4.CreateFromQuaternion(rb.defaultRotation) * Matrix4x4.CreateTranslation(rb.defaultPosition);

            motionState = new DefaultMotionState(GetMatrix(mat));
            CollisionShape collisionShape;
            switch (desc.Shape)
            {
                case PMX_RigidBodyShape.Sphere:
                    collisionShape = new SphereShape(desc.Dimensions.X);
                    break;
                case PMX_RigidBodyShape.Capsule:
                    collisionShape = new CapsuleShape(desc.Dimensions.X, desc.Dimensions.Y);
                    break;
                case PMX_RigidBodyShape.Box:
                default:
                    collisionShape = new BoxShape(GetVector3(desc.Dimensions));
                    break;
            }
            float mass = desc.Mass;
            BulletSharp.Math.Vector3 localInertia = new BulletSharp.Math.Vector3();
            if (desc.Type == 0) mass = 0;
            else
            {
                collisionShape.CalculateLocalInertia(mass, out localInertia);
            }
            var rigidbodyInfo = new RigidBodyConstructionInfo(mass, motionState, collisionShape, localInertia);
            rigidbodyInfo.Friction = desc.Friction;
            rigidbodyInfo.LinearDamping = desc.LinearDamp;
            rigidbodyInfo.AngularDamping = desc.AngularDamp;
            rigidbodyInfo.Restitution = desc.Restitution;

            rb.rigidBody = new RigidBody(rigidbodyInfo);
            rb.rigidBody.ActivationState = ActivationState.DisableDeactivation;
            rb.rigidBody.SetSleepingThresholds(0, 0);
            if (desc.Type == PMX_RigidBodyType.Kinematic)
            {
                rb.rigidBody.CollisionFlags |= CollisionFlags.KinematicObject;
            }
            world.AddRigidBody(rb.rigidBody, 1 << desc.CollisionGroup, desc.CollisionMask);
        }

        public void AddJoint(PhysicsJoint joint, PhysicsRigidBody r1, PhysicsRigidBody r2, PMX_Joint desc)
        {

            var t0 = Matrix4x4.CreateFromQuaternion(ToQuaternion(desc.Rotation)) * Matrix4x4.CreateTranslation(desc.Position);
            Matrix4x4.Invert(t0, out var res);
            Matrix4x4.Invert(Matrix4x4.CreateFromQuaternion(r1.defaultRotation) * Matrix4x4.CreateTranslation(r1.defaultPosition), out var t1);
            Matrix4x4.Invert(Matrix4x4.CreateFromQuaternion(r2.defaultRotation) * Matrix4x4.CreateTranslation(r2.defaultPosition), out var t2);
            t1 = t0 * t1;
            t2 = t0 * t2;

            var j = new Generic6DofSpringConstraint(r1.rigidBody, r2.rigidBody, GetMatrix(t1), GetMatrix(t2), true);
            joint.constraint = j;
            j.LinearLowerLimit = GetVector3(desc.LinearMinimum);
            j.LinearUpperLimit = GetVector3(desc.LinearMaximum);
            j.AngularLowerLimit = GetVector3(desc.AngularMinimum);
            j.AngularUpperLimit = GetVector3(desc.AngularMaximum);

            S(0, desc.LinearSpring.X);
            S(1, desc.LinearSpring.Y);
            S(2, desc.LinearSpring.Z);
            S(3, desc.AngularSpring.X);
            S(4, desc.AngularSpring.Y);
            S(5, desc.AngularSpring.Z);
            void S(int index, float f)
            {
                if (f != 0.0f)
                {
                    j.EnableSpring(index, true);
                    j.SetStiffness(index, f);
                }
                else
                {
                    j.EnableSpring(index, false);
                }
            }

            world.AddConstraint(joint.constraint);
        }

        public void Simulation(double time)
        {
            world.StepSimulation(time);
        }

        public void ResetRigidBody(PhysicsRigidBody rb, Vector3 position, Quaternion rotation)
        {
            var worldTransform = GetMatrix(Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position));
            var rigidBody = rb.rigidBody;
            rigidBody.MotionState.SetWorldTransform(ref worldTransform);
            rigidBody.CenterOfMassTransform = worldTransform;
            rigidBody.InterpolationWorldTransform = worldTransform;
            rigidBody.InterpolationWorldTransform = worldTransform;
            rigidBody.AngularVelocity = new BulletSharp.Math.Vector3();
            rigidBody.LinearVelocity = new BulletSharp.Math.Vector3();
            rigidBody.InterpolationAngularVelocity = new BulletSharp.Math.Vector3();
            rigidBody.InterpolationLinearVelocity = new BulletSharp.Math.Vector3();
            rigidBody.ClearForces();
        }

        public void ResetRigidBody(PhysicsRigidBody rb, Matrix4x4 mat)
        {
            var worldTransform = GetMatrix(mat);
            var rigidBody = rb.rigidBody;
            rigidBody.MotionState.SetWorldTransform(ref worldTransform);
            rigidBody.CenterOfMassTransform = worldTransform;
            rigidBody.InterpolationWorldTransform = worldTransform;
            rigidBody.InterpolationWorldTransform = worldTransform;
            rigidBody.AngularVelocity = new BulletSharp.Math.Vector3();
            rigidBody.LinearVelocity = new BulletSharp.Math.Vector3();
            rigidBody.InterpolationAngularVelocity = new BulletSharp.Math.Vector3();
            rigidBody.InterpolationLinearVelocity = new BulletSharp.Math.Vector3();
            rigidBody.ClearForces();
        }

        public void MoveRigidBody(PhysicsRigidBody rb, Matrix4x4 mat)
        {
            rb.rigidBody.MotionState.WorldTransform = GetMatrix(mat);
        }

        public void RemoveRigidBody(PhysicsRigidBody rb)
        {
            world.RemoveRigidBody(rb.rigidBody);
            rb.rigidBody.Dispose();
        }

        public void RemoveJoint(PhysicsJoint joint)
        {
            world.RemoveConstraint(joint.constraint);
            joint.constraint.Dispose();
        }

        public BulletSharp.Math.Vector3 GetVector3(Vector3 v)
        {
            return new BulletSharp.Math.Vector3(v.X, v.Y, v.Z);
        }

        public BulletSharp.Math.Matrix GetMatrix(Matrix4x4 mat)
        {
            BulletSharp.Math.Matrix m = new BulletSharp.Math.Matrix();
            m.M11 = mat.M11;
            m.M12 = mat.M12;
            m.M13 = mat.M13;
            m.M14 = mat.M14;
            m.M21 = mat.M21;
            m.M22 = mat.M22;
            m.M23 = mat.M23;
            m.M24 = mat.M24;
            m.M31 = mat.M31;
            m.M32 = mat.M32;
            m.M33 = mat.M33;
            m.M34 = mat.M34;
            m.M41 = mat.M41;
            m.M42 = mat.M42;
            m.M43 = mat.M43;
            m.M44 = mat.M44;
            return m;
        }

        public static Quaternion ToQuaternion(Vector3 angle)
        {
            return Quaternion.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z);
        }
    }
}
