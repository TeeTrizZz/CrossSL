#pragma warning disable 0649
using CrossSL.Meta;
using Fusee.Math;

namespace Example
{
    [xSLTarget(xSLTarget.GLSLMix.V110)]
    [xSLDebug(xSLDebug.IgnoreShader | xSLDebug.PreCompile | xSLDebug.SaveToFile)]
    public class ComplexShader : xSLShader
    {
        [xSLUniform] private float water_dist;
        [xSLUniform] private float timer;
        [xSLUniform] private sampler2D colorMap;
        [xSLUniform] private sampler2D reflectedColorMap;
        [xSLUniform] private sampler2D stencilMap;
        [xSLVarying] private float4 texcoord;

        [xSLConst] private const int gaussRadius = 11;
        [xSLConst] private const float PI = 3.141592653589793238462643383279f;

        [xSLConst] private readonly float[] gaussFilter =
        {
            0.0402f, 0.0623f, 0.0877f, 0.1120f, 0.1297f, 0.1362f, 0.1297f, 0.1120f, 0.0877f, 0.0623f, 0.0402f
        };

        public override void VertexShader()
        {
            for (int x = 0; x < 10; x++)
                texcoord = xslMultiTexCoord0;
            xslPosition = xslProjectionMatrix*xslVertex;

            var z = MathHelper.Sin(10f*5f*3f);
        }

        public override void FragmentShader()
        {
            var water_color = new float3(0.0f, 0.4f, 0.2f); //0.22,0.2 (now uniform through config)
            float in_water; //'boolean'

            float4 color;
            float4 stencil = Texture2D(stencilMap, texcoord.rg);

            in_water = stencil.r > 0.05f ? 1.0f : 0.0f;

            //distortion begin
            float x_scale = 1.0f;
            float z_scale = 1.0f;

            float used_timer = timer;
            float time_scale = 2.0f; //2.0
            float size_scale = 1.6f*6.3f; //also dependent on radius

            if (stencil.r <= 0.15f)
            {
                size_scale *= 6.0f;
                time_scale *= 1.5f;
            }
            else
            {
                size_scale *= stencil.r;
            }

            //timer needs to be 'in period'
            if (stencil.r >= 0.5f)
            {
                x_scale = 0.995f +
                          MathHelper.Sin(2.0f*time_scale*3.14159f*used_timer -
                                         MathHelper.Sin(0.5f*size_scale*3.14159f*stencil.g) +
                                         (size_scale*3.14159f*stencil.g))/100.0f;
            }

            z_scale = 0.995f +
                      (MathHelper.Sin(MathHelper.Sin(time_scale*3.14159f*used_timer) +
                                      1.5f*MathHelper.Sin(0.8f*size_scale*3.14159f*stencil.b))/150.0f);

            var disturbed = new float2(x_scale*texcoord.x, z_scale*texcoord.y);

            float4 reflection = Texture2D(reflectedColorMap, disturbed.rg);
            //if (x_scale + z_scale > 2.00099) reflection *= 1.8; //to monitor effects...!

            time_scale = 3.0f; //2.0
            size_scale = 2.4f*6.3f*stencil.r;

            //timer needs to be 'in period'
            if (stencil.r >= 0.5f)
            {
                //- Math.Sin(0.25*size_scale*3.14159*stencil.g)
                x_scale = 0.995f +
                          (MathHelper.Sin(2.0f*time_scale*3.14159f*used_timer -
                                          MathHelper.Sin(0.25f*size_scale*3.14159f*stencil.g) +
                                          size_scale*3.14159f*stencil.g)/100.0f); //scales btw 0.995 and 1.005
            }
            z_scale = 0.995f +
                      (MathHelper.Sin(MathHelper.Sin(time_scale*3.14159f*used_timer) +
                                      1.5f*MathHelper.Sin(size_scale*3.14159f*stencil.b))/100.0f);
            var disturbed_2 = new float2(x_scale*texcoord.x, z_scale*texcoord.y);
            //distortion end

            float4 reflection_2 = Texture2D(reflectedColorMap, disturbed_2.xy);


            reflection = (reflection + reflection_2)/2.0f;

            //'refraction'(for all under-water)
            if (in_water > 0.05f)
            {
                float look_up_range = 0.008f; //0.005 //0.008
                //costs performance! (masking to avoid outside water look-ups, alternative another scene clipping)
                if (Texture2D(stencilMap, new float2(disturbed.r + look_up_range, disturbed.y + look_up_range)).x >
                    0.001f &&
                    Texture2D(stencilMap, new float2(disturbed.r - look_up_range, disturbed.y - look_up_range)).x >
                    0.001f &&
                    Texture2D(stencilMap, new float2(disturbed.r, disturbed.y)).x > 0.001f)
                {
                    color = Texture2D(colorMap, disturbed.rg); //drunken effect without stencil if
                }
                else
                {
                    color = Texture2D(colorMap, texcoord.xy);
                }
            }
            else
            {
                color = Texture2D(colorMap, texcoord.xy);
            }

            //combine reflection and scene at water surfaces
            //modify reflection in distance?
            float reflection_strength = 0.3f*(stencil.r - 0.1f); //0.4, 0.55, 0.1, 0.14, 0.16, 0.17, 0.5
            float disable_refl = stencil.r - 0.1f;

            if (disable_refl <= 0.0f) disable_refl = 0.0f; //no reflection

            //times inverted color.x for a stronger reflection in darker water parts!
            //used to be 8.0, 6.0, 3.5
            var reflection_color = new float3(1.0f, 1.0f, 1.0f);
            reflection_color = reflection_strength*disable_refl*reflection.xyz;
            // * reflection.xyb * in_water * (1.0-(color.x*color.y*color.z));

            //more color in darker water in relation to the reflection
            //color darkened
            float difference = (reflection_color.x + reflection_color.y + reflection_color.z)/3.0f -
                               (color.x + color.y + color.z)/5.5f; //5.5
            if (difference < 0.0f) difference = 0.0f;
            float3 regular_color = color.xyz*(1.0f - in_water*reflection_strength) + (in_water*(difference*water_color));

            var surface_effects = 10f*0.5f*20f*regular_color.x;
            if (surface_effects > 0.0f)
            {
                //"waves"
                float t = 3.0f*(PI*0.1f*timer) + 12.0f;
                float u = (1.1f*stencil.g);
                float v = (1.1f*stencil.b);

                //water "height" bumps
                //Math.Sin(PI*t*v) -> also for size of the "bumps"
                //20.0*t -> "speed"
                float rsx =
                    (MathHelper.Sin(0.9f*MathHelper.Sin(PI*t*v) + 0.7f*MathHelper.Sin(PI*t*v) + 18.1f*PI*stencil.g) +
                     MathHelper.Sin(t*t + MathHelper.Sin(PI*t*v*u) + 26.3f*PI*stencil.g))*0.05f;
                float rsz =
                    (MathHelper.Sin(0.6f*MathHelper.Sin(PI*t*u) + 0.8f*MathHelper.Sin(PI*t*u) + 16.4f*PI*stencil.b) +
                     MathHelper.Sin(t*t + MathHelper.Sin(PI*t*u + u) + 32.2f*PI*stencil.b))*0.05f;

                rsx += 0.15f;
                rsz += 0.15f;

                float fresn = MathHelper.Clamp(4.0f/water_dist, 0.0f, 1.0f);

                rsx = MathHelper.Clamp(rsx, 0.0f, 1.0f);
                rsz = MathHelper.Clamp(rsz, 0.0f, 1.0f);

                //Math.Sinc filter (alternative, not used yet)

                float tm = (timer/550.0f) + 0.255f; //0.45 0.26 0.28
                if (tm > 0.28f) tm = 0.28f - (timer/55.0f); //HMM

                float pow3 = (tm + rsx + rsz)*(tm + rsx + rsz)*(tm + rsx + rsz);
                rsx *= 1.6f + 0.7f*MathHelper.Sin(16.6f*(tm + rsx + rsz))/pow3;
                rsz *= 1.3f + 0.7f*MathHelper.Sin(16.1f*(tm + rsx + rsz))/pow3;

                rsx = 1.0f - rsx;
                rsz = 1.0f - rsz;

                //surface color increase
                if (rsx + rsz > 1.9f && rsx + rsz < 1.999f)
                {
                    rsx *= 1.05f;
                    rsz *= 1.05f;
                    if (rsx + rsz > 1.999f)
                    {
                        rsx *= 1.07f;
                        rsz *= 1.07f;
                        if (rsx + rsz > 1.9993f)
                        {
                            rsx *= 1.1f;
                            rsz *= 1.1f;
                        }
                    }
                }

                float increase = rsx + rsz;
                float mult = 5.0f;
                float max = 1.15f;
                increase = mult*(increase - 0.9f);
                if (increase > mult*max) increase = 0.0f;
                increase = MathHelper.Clamp(increase, 1.0f, 2.0f);

                rsx *= increase;
                rsz *= increase;

                if (increase > 1) rsx = 100;

                rsx = 1.0f - rsx;
                rsz = 1.0f - rsz;

                reflection_color *= new float3(0.6f, 0.95f, 0.95f);
                reflection_color *= 0.8f;
                reflection_color = 1.1f*reflection_color +
                                   fresn*(1.0f - reflection_strength)*
                                   (reflection_color*1.5f*rsx + reflection_color*1.5f*rsz);

                float count = 1.0f;
                for (int i = -3; i < 3; i++)
                {
                    float2 uv = disturbed.rg;
                    uv.y += 0.007f*i;


                    float3 col = Texture2D(reflectedColorMap, uv).xyz;
                    if (reflection_color.x < 0.01f) col = new float3();

                    float str = (col.x + col.y + col.z)/3.0f;
                    str = str*str - 0.2f;

                    if (col.x + col.y + col.z > 2.3f)
                    {
                        // reflection_color += MathHelper.Clamp((str*col), 0.0f, 3.0f); // * float3(1.0, 0.0, 0.0);
                        count++;
                    }
                }
                reflection_color /= 6;

                if (count > 1) reflection_color.xyz = new float3(1.0f, 0.0f, 0.0f);
            }

            float4 out_color = new float4(regular_color, 1.0f) + new float4(reflection_color, 1.0f);

            //TEST
            var add = new float3(0.0f, 0.0f, 0.0f);

            if (stencil.r > 0.1f)
            {
                var uShift = new float2(0.005f, 0.0f);

                float2 texCoord = texcoord.xy - gaussRadius/2.0f*uShift;

                for (int i = 0; i < gaussRadius; ++i)
                {
                    add += gaussFilter[i]*Texture2D(colorMap, texCoord).xyz;
                    texCoord += uShift;
                }

                uShift = new float2(0.0f, 0.007f);

                texCoord = texcoord.xy - gaussRadius/2*uShift;
                for (int i = 0; i < gaussRadius; ++i)
                {
                    add += gaussFilter[i]*Texture2D(colorMap, texCoord).xyz;
                    texCoord += uShift;
                }
            }
            //TEST END

            xslFragColor = out_color*new float4(1.5f, 1.3f, 1.0f, 1.0f);
        }
    }
}

#pragma warning restore 0649