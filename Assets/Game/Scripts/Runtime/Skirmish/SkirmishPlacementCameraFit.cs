using System;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishPlacementCameraFit
    {
        public static bool TryViewportRect(Rect screenNormalized,Rect cameraPixels,float screenWidth,float screenHeight,out Rect viewport)
        {
            viewport=default;
            if(!Valid(screenNormalized)||!Finite(screenWidth)||!Finite(screenHeight)||screenWidth<=0||screenHeight<=0||
                !Finite(cameraPixels.xMin)||!Finite(cameraPixels.yMin)||!Finite(cameraPixels.xMax)||!Finite(cameraPixels.yMax)||cameraPixels.width<=0||cameraPixels.height<=0)return false;
            float minX=Mathf.Max(screenNormalized.xMin*screenWidth,cameraPixels.xMin),maxX=Mathf.Min(screenNormalized.xMax*screenWidth,cameraPixels.xMax);
            float minY=Mathf.Max(screenNormalized.yMin*screenHeight,cameraPixels.yMin),maxY=Mathf.Min(screenNormalized.yMax*screenHeight,cameraPixels.yMax);
            if(maxX<=minX||maxY<=minY)return false;
            viewport=Rect.MinMaxRect((minX-cameraPixels.xMin)/cameraPixels.width,(minY-cameraPixels.yMin)/cameraPixels.height,
                (maxX-cameraPixels.xMin)/cameraPixels.width,(maxY-cameraPixels.yMin)/cameraPixels.height);
            return Valid(viewport);
        }
        public static bool TryFit(Vector3[] points, Quaternion rotation, float fieldOfView, float aspect,
            Rect viewport, float near, float far, out Vector3 position, float minimumHeight=float.NegativeInfinity)
        {
            position=default;
            if(points==null||points.Length<4||!Finite(aspect)||aspect<=0||!Finite(fieldOfView)||fieldOfView<=1||fieldOfView>=179||
                !Finite(near)||near<=0||!Finite(far)||far<=near||!Valid(viewport)||
                !Finite(rotation.x)||!Finite(rotation.y)||!Finite(rotation.z)||!Finite(rotation.w)||Mathf.Abs(Quaternion.Dot(rotation,rotation)-1)>.001f)return false;
            Vector3 min=points[0],max=min;
            foreach(var p in points){if(!Finite(p))return false;min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
            if((max-min).sqrMagnitude<=.000001f)return false;
            var center=(min+max)*.5f;var right=rotation*Vector3.right;var up=rotation*Vector3.up;var forward=rotation*Vector3.forward;
            float ty=Mathf.Tan(fieldOfView*Mathf.Deg2Rad*.5f),tx=ty*aspect;
            float left=viewport.xMin*2-1,rightEdge=viewport.xMax*2-1,bottom=viewport.yMin*2-1,top=viewport.yMax*2-1;
            float cx=(left+rightEdge)*.5f,cy=(bottom+top)*.5f,distance=near;
            var cameraDirection=-forward-right*(tx*cx)-up*(ty*cy);
            if(!float.IsNegativeInfinity(minimumHeight))
            {
                if(!Finite(minimumHeight)||cameraDirection.y<=0)return false;
                distance=Mathf.Max(distance,(minimumHeight-center.y)/cameraDirection.y);
            }
            foreach(var p in points)
            {
                var delta=p-center;float x=Vector3.Dot(delta,right)/tx,y=Vector3.Dot(delta,up)/ty,z=Vector3.Dot(delta,forward);
                distance=Mathf.Max(distance,(left*z-x)/(cx-left),(x-rightEdge*z)/(rightEdge-cx),
                    (bottom*z-y)/(cy-bottom),(y-top*z)/(top-cy),near-z);
            }
            // A numerical epsilon protects the exact calculated boundary, not a guessed geometry margin.
            distance+=Mathf.Max(.0001f,distance*.000001f);
            var candidate=center-forward*distance-right*(tx*cx*distance)-up*(ty*cy*distance);
            if(!Finite(candidate)||!Contains(points,candidate,rotation,fieldOfView,aspect,viewport,near,far))return false;
            position=candidate;return true;
        }
        public static bool Contains(Vector3[] points,Vector3 position,Quaternion rotation,float fov,float aspect,Rect viewport,float near,float far)
        {
            if(points==null||points.Length<4||!Finite(position)||!Valid(viewport)||!Finite(aspect)||aspect<=0||!Finite(fov)||fov<=1||fov>=179||!Finite(near)||near<=0||!Finite(far)||far<=near)return false;
            var inverse=Quaternion.Inverse(rotation);float ty=Mathf.Tan(fov*Mathf.Deg2Rad*.5f);
            foreach(var p in points)
            {
                var v=inverse*(p-position);if(!Finite(v)||v.z<near||v.z>far)return false;
                float x=.5f+.5f*v.x/(v.z*ty*aspect),y=.5f+.5f*v.y/(v.z*ty);
                if(x<viewport.xMin-.00001f||x>viewport.xMax+.00001f||y<viewport.yMin-.00001f||y>viewport.yMax+.00001f)return false;
            }
            return true;
        }
        static bool Valid(Rect v)=>Finite(v.xMin)&&Finite(v.yMin)&&Finite(v.xMax)&&Finite(v.yMax)&&v.xMin>=0&&v.yMin>=0&&v.xMax<=1&&v.yMax<=1&&v.width>.01f&&v.height>.01f;
        static bool Finite(float v)=>float.IsFinite(v);
        static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
    }
}
