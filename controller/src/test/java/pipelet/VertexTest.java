package pipelet;

import org.junit.Test;
import static org.junit.Assert.*;

public class VertexTest {
    @Test
    public void testBasic() {
        Vertex node = new Vertex("isonf", "n1");
        assertEquals(node.requiredCores(), 1.0, 1e-15);
        assertEquals(node.requiredMemory(), 0.0, 1e-15);
    }
}
