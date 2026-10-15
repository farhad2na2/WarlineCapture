using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering.Contracts
{
    // Engineering math only. Callers must prove the exact drawn indexed geometry,
    // blade/pivot/axis and parent provenance. This does not discover or certify them.
    public readonly struct SkirmishRigidRotorExtent
    {
        public readonly byte Axis;
        public readonly float MinimumAxial,MaximumAxial,MaximumRadius;
        public SkirmishRigidRotorExtent(byte axis,float min,float max,float radius)
        {Axis=axis;MinimumAxial=min;MaximumAxial=max;MaximumRadius=radius;}
    }
    public static class SkirmishRigidRotorSweep
    {
        // Editor/baker input must contain precisely the actual indexed vertices of
        // the drawn submesh, not mesh.bounds, culling extents or unused vertices.
        public static bool TryMeasure(IReadOnlyList<Vector3> indexedVertices,Matrix4x4 meshToBlade,
            byte axis,out SkirmishRigidRotorExtent extent)
        {
            extent=default;
            if(indexedVertices==null||indexedVertices.Count==0||axis>2||!Affine(meshToBlade))return false;
            Vector3 a=UnitAxis(axis);float min=float.PositiveInfinity,max=float.NegativeInfinity,radius=0;
            foreach(Vector3 vertex in indexedVertices)
            {
                if(!Finite(vertex))return false;
                Vector3 p=meshToBlade.MultiplyPoint3x4(vertex);
                if(!Finite(p))return false;
                float axial=Vector3.Dot(p,a);Vector3 radial=p-a*axial;
                double squared=(double)radial.x*radial.x+(double)radial.y*radial.y+(double)radial.z*radial.z;
                double r=Math.Sqrt(squared);if(double.IsNaN(r)||double.IsInfinity(r)||r>float.MaxValue)return false;
                float roundedRadius=(float)r;
                if(roundedRadius<r)roundedRadius=Next(roundedRadius,true);
                if(!Finite(roundedRadius))return false;
                min=Mathf.Min(min,axial);max=Mathf.Max(max,axial);radius=Mathf.Max(radius,roundedRadius);
            }
            extent=new SkirmishRigidRotorExtent(axis,min,max,radius);return true;
        }
        // Union over theta in [0,2pi] of B*RotateAxis(theta)*p. A full local cylinder
        // conservatively contains every measured indexed vertex. Support functions
        // transform that cylinder directly, including shear/mirror/nonuniform scale.
        // Do not transform a guessed sphere radius by a single lossyScale value.
        public static bool TryWorldEnvelope(in SkirmishRigidRotorExtent extent,Matrix4x4 bladeToWorld,out Bounds bounds)
        {
            bounds=default;
            if(extent.Axis>2||!Finite(extent.MinimumAxial)||!Finite(extent.MaximumAxial)||!Finite(extent.MaximumRadius)||
                extent.MinimumAxial>extent.MaximumAxial||extent.MaximumRadius<0||!Affine(bladeToWorld))return false;
            Vector3 a=UnitAxis(extent.Axis),minimum=default,maximum=default;
            for(int i=0;i<3;i++)
            {
                double x=bladeToWorld[i,0],y=bladeToWorld[i,1],z=bladeToWorld[i,2];
                double along=x*a.x+y*a.y+z*a.z;
                double radialSquared=extent.Axis==0?y*y+z*z:extent.Axis==1?x*x+z*z:x*x+y*y;
                double spread=extent.MaximumRadius*Math.Sqrt(radialSquared);
                double first=along*extent.MinimumAxial,second=along*extent.MaximumAxial;
                double lo=bladeToWorld[i,3]+Math.Min(first,second)-spread;
                double hi=bladeToWorld[i,3]+Math.Max(first,second)+spread;
                if(double.IsNaN(lo)||double.IsInfinity(lo)||double.IsNaN(hi)||double.IsInfinity(hi)||lo < -float.MaxValue||hi>float.MaxValue)return false;
                float lower=(float)lo,upper=(float)hi;
                if(lower>lo)lower=Next(lower,false);
                if(upper<hi)upper=Next(upper,true);
                minimum[i]=lower;maximum[i]=upper;
            }
            Vector3 size=maximum-minimum;if(!Finite(size))return false;
            bounds.SetMinMax(minimum,maximum);
            // Bounds stores float center/extents; pad only for that arithmetic's
            // cancellation/rounding, independently of any camera fit policy.
            float magnitude=Mathf.Max(Mathf.Max(Mathf.Abs(minimum.x),Mathf.Abs(maximum.x)),
                Mathf.Max(Mathf.Max(Mathf.Abs(minimum.y),Mathf.Abs(maximum.y)),Mathf.Max(Mathf.Abs(minimum.z),Mathf.Abs(maximum.z))));
            bounds.Expand(Mathf.Max(1e-6f,magnitude*2e-6f));
            return Finite(bounds.center)&&Finite(bounds.size);
        }
        static Vector3 UnitAxis(byte axis)=>axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
        static float Next(float value,bool up)
        {
            if(value==0)return up?float.Epsilon:-float.Epsilon;
            int bits=BitConverter.SingleToInt32Bits(value);
            return BitConverter.Int32BitsToSingle(bits+((value>0)==up?1:-1));
        }
        static bool Affine(Matrix4x4 matrix)
        {
            for(int i=0;i<16;i++)if(!Finite(matrix[i]))return false;
            return matrix[3,0]==0&&matrix[3,1]==0&&matrix[3,2]==0&&matrix[3,3]==1;
        }
    }
}
