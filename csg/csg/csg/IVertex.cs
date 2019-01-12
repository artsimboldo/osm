namespace csg
{
    public interface IVertex
    {
        Vector3 pos { get; set; }
        IVertex clone();
        void flip();
        IVertex interpolate(IVertex other, double t);
    }
}