package pipelet;

import org.junit.Test;

import static org.junit.Assert.*;

public class ServerTest {

    @Test
    public void testAvailableCores() throws Exception {
        Server s = new Server(18.0, 0.0);
        assertEquals(s.availableCores(), 18.0, 1e-15);
    }

    @Test
    public void testAvailableMemory() throws Exception {
        Server s = new Server(18.0, 0.0);
        assertEquals(s.availableMemory(), 0.0, 1e-15);
    }

    @Test
    public void testSatisfy() throws Exception {
        Server s = new Server(18.0, 0.0);
        assertTrue(s.satisfy(16.0, 0.0));
        assertFalse(s.satisfy(1.0, 1.0));
        assertFalse(s.satisfy(19.0, 0.0));
    }

    @Test
    public void testConsume() throws Exception {
        Server s = new Server(18.0, 0.0);
        assertTrue(s.consume(10.0, 0.0));
        assertEquals(s.availableCores(), 8.0, 1e-15);
        assertFalse(s.consume(10.0, 0.0));
        assertEquals(s.availableCores(), 8.0, 1e-15);
    }
}