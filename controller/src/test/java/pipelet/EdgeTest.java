package pipelet;

import static org.junit.Assert.*;
import org.junit.Test;

public class EdgeTest {
    @Test
    public void testBasic() {
        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Edge e = new Edge(n1, n2);
        assertEquals(e.getFilter(), null);
    }

    @Test
    public void testFilter() {
        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Edge e = new Edge(n1, n2, "ip host 10.0.0.1");
        assertEquals(e.getFilter(), "ip host 10.0.0.1");
    }
}
