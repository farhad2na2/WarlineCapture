using System;
namespace Game.Simulation
{
    public enum CameraFocusActionKind { ClearIntroSettle, ClearPerspective, CompleteZoom, InstantPerspective, SmoothPerspective, MoveFocus, SmoothFocus, ClearFocus }
    public struct CameraFocusAction { public CameraFocusActionKind Kind; public float A, B, C, D, Duration; }
    public struct CameraFocusRequest
    {
        public bool ExplicitPerspective, Smooth, ExplicitYaw;
        public byte TacticalReveal;
        public float Height, Pitch, Yaw, FieldOfView, Duration, FocusX, FocusY, FocusZ, RevealYaw;
        public float NormalHeight, NormalPitch, NormalYaw, NormalFieldOfView;
    }
    public static class CameraFocusSequence
    {
        static void Send(Action<CameraFocusAction> emit, CameraFocusActionKind kind, float a=0, float b=0, float c=0, float d=0, float duration=0) =>
            emit(new CameraFocusAction { Kind=kind, A=a, B=b, C=c, D=d, Duration=duration });
        public static void Tactical(byte kind, float duration, float height, float pitch, float yaw, float fov, bool explicitYaw, float chosenYaw, Action<CameraFocusAction> emit)
        {
            if (emit == null) throw new ArgumentNullException(nameof(emit));
            Send(emit,CameraFocusActionKind.ClearIntroSettle);
            bool squad=kind==2, bazaar=kind==3, restore=kind==4, combat=kind==5;
            float h=restore?height:bazaar?15f:combat?12f:9f;
            float p=restore?pitch:bazaar?35f:30f;
            float y=explicitYaw?chosenYaw:restore?yaw:0f;
            float f=restore?fov:bazaar?50f:combat?38f:squad?42f:38f;
            if (duration>0f) { Send(emit,CameraFocusActionKind.CompleteZoom); Send(emit,CameraFocusActionKind.SmoothPerspective,h,p,y,f,duration); }
            else { Send(emit,CameraFocusActionKind.InstantPerspective,h,p,y,f); Send(emit,CameraFocusActionKind.CompleteZoom); }
        }
        public static void Emit(CameraFocusRequest r, Action<CameraFocusAction> emit)
        {
            if (emit == null) throw new ArgumentNullException(nameof(emit));
            if (r.ExplicitPerspective)
            {
                Send(emit,CameraFocusActionKind.ClearIntroSettle); Send(emit,CameraFocusActionKind.ClearPerspective); Send(emit,CameraFocusActionKind.CompleteZoom);
                Send(emit,r.Smooth?CameraFocusActionKind.SmoothPerspective:CameraFocusActionKind.InstantPerspective,r.Height,r.Pitch,r.Yaw,r.FieldOfView,r.Duration);
            }
            else if (r.TacticalReveal!=0)
                Tactical(r.TacticalReveal,r.Smooth?(0.6f>r.Duration?0.6f:r.Duration):0f,r.NormalHeight,r.NormalPitch,r.NormalYaw,r.NormalFieldOfView,r.ExplicitYaw,r.RevealYaw,emit);
            if (r.Smooth) Send(emit,CameraFocusActionKind.SmoothFocus,r.FocusX,r.FocusY,r.FocusZ,duration:r.Duration);
            else { Send(emit,CameraFocusActionKind.MoveFocus,r.FocusX,r.FocusY,r.FocusZ); Send(emit,CameraFocusActionKind.ClearFocus); }
        }
    }
}
