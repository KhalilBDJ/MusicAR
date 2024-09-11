class Peak
{
    public float amplitude;
    public int index;
 
    public Peak()
    {
        amplitude = 0f;
        index = -1;
    }
 
    public Peak(float _frequency, int _index)
    {
        amplitude = _frequency;
        index = _index;
    }
}