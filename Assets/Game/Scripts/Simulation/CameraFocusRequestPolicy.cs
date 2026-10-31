namespace Game.Simulation
{
    public struct CameraFocusRequestPolicyInput
    {
        public bool Opening, BasinScenario, S002, S004, Enemy, AuthoredOpening, HasMissionView;
        public float BaseX, BaseY, BaseZ, MinimumX, MinimumY, MinimumZ, MaximumX, MaximumY, MaximumZ;
        public float CampaignYaw, CampaignHeight, ViewHeight, ViewPitch, ViewYaw, ViewFieldOfView;
    }
    public struct CameraFocusRequestPolicyResult
    {
        public float X, Y, Z, Height, Pitch, Yaw, FieldOfView;
    }
    public static class CameraFocusRequestPolicy
    {
        public static CameraFocusRequestPolicyResult Calculate(CameraFocusRequestPolicyInput i)
        {
            float x=i.BaseX,y=i.BaseY,z=i.BaseZ, distance=40f;
            if (i.AuthoredOpening)
            {
                x=(i.MinimumX+i.MaximumX)*.5f; y=(i.MinimumY+i.MaximumY)*.5f; z=(i.MinimumZ+i.MaximumZ)*.5f;
                float dx=i.MaximumX-i.MinimumX,dz=i.MaximumZ-i.MinimumZ;
                float extent=dx>dz?dx:dz;
                distance=extent*1.3f+30f; distance=distance>60f?distance:60f; distance=distance<180f?distance:180f;
                if(i.S004)distance=i.CampaignHeight;
            }
            float height=i.AuthoredOpening?distance:i.S004?i.CampaignHeight:i.S002?100f:i.Opening&&i.BasinScenario?60f:i.Opening?40f:55f;
            if(i.HasMissionView)height=i.ViewHeight;
            if(i.S004){if(!i.Enemy){x-=12f;z-=8f;}}
            else if(!i.AuthoredOpening){if(i.S002)x+=i.Enemy?-30f:30f;else if(i.Opening){x+=20f;z+=6f;}else z+=12f;}
            return new CameraFocusRequestPolicyResult { X=x,Y=y,Z=z,Height=height,
                Pitch=i.HasMissionView?i.ViewPitch:i.S004?50f:58f,
                Yaw=i.HasMissionView?i.ViewYaw:i.S004?i.CampaignYaw:0f,
                FieldOfView=i.HasMissionView?i.ViewFieldOfView:i.S004?50f:60f };
        }
    }
}
