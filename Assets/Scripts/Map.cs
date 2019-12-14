/*
 * Map.java
 */

/**
 * An object that contains map data.
 */

public class Map
{

    // --- fields ---

    private DynamicArray.OfBoolean map;
    private int[] start;
    private int[] finish;

    // --- accessors ---

    public bool inBounds(int[] p) { return map.inBounds(p); }

    public bool isOpen(int[] p) { return map.get(p); }
    public void setOpen(int[] p, bool b) { map.set(p, b); } // generator only

    public int[] getStart() { return start; }
    public int[] getFinish() { return finish; }

    public void setStart(int[] start) { this.start = start; } // generator only
    public void setFinish(int[] finish) { this.finish = finish; }

    // --- construction ---

    public Map(int dimSpace, OptionsMap om, int seed)
    {

        int[] limits = DynamicArray.makeLimits(om.size);

        map = new DynamicArray.OfBoolean(dimSpace, limits);
        // elements start out false, which is correct

        // start and finish are produced by the generation algorithm

        new MapGenerator(this, limits, om, seed).generate();
    }

}

