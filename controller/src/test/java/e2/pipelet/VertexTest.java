package e2.pipelet;

import org.junit.Test;

import static org.junit.Assert.assertEquals;

public class VertexTest {
    @Test
    public void testBasic() {
        Vertex node = new Vertex("isonf", "n1");
        assertEquals(1.0, node.requiredCores(), 1e-15);
        assertEquals(0.0, node.requiredMemory(), 1e-15);
    }
}
