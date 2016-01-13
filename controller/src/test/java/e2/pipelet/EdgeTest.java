package e2.pipelet;

import org.junit.Test;

import static org.junit.Assert.assertEquals;

public class EdgeTest {
    @Test
    public void testFilter() {
        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Edge e = new Edge(n1, n2, 0, 0, "ip host 10.0.0.1");
        assertEquals("ip host 10.0.0.1", e.getFilter());
    }
}
