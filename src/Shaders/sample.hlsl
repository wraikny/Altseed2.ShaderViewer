struct PS_INPUT
{
  float4  Position : SV_POSITION;
  float4  Color    : COLOR0;
  float2  UV1 : UV0;
  float2  UV2 : UV1;
};

float4 main(PS_INPUT input) : SV_TARGET
{
  return float4(input.UV1, 1.0, 1.0);
}
