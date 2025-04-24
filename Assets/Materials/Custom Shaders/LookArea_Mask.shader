Shader "Custom/LookArea_Mask"
{
    SubShader
    {
        Tags{"Queue" = "Transparent+1"}

        Pass{
            Blend Zero One
        }
    }
}
