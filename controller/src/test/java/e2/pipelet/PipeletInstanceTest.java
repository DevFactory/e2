package e2.pipelet;

import org.junit.Test;

import java.util.ArrayList;
import java.util.List;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;

public class PipeletInstanceTest {

    @Test
    public void testClearPlacement() throws Exception {
        List<Server> servers = new ArrayList<Server>();
        for (int i = 0; i < 10; ++i) {
            servers.add(new Server(16.0, 128.0));
        }

        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Vertex n3 = new Vertex("isonf", "n3");
        List<Vertex> nodes = new ArrayList<Vertex>();
        nodes.add(n1);
        nodes.add(n2);
        nodes.add(n3);

        Edge e1 = new Edge(n1, n3, 0, 0, "filter1");
        Edge e2 = new Edge(n2, n3, 0, 0, "filter2");
        List<Edge> edges = new ArrayList<Edge>();
        edges.add(e1);
        edges.add(e2);

        PipeletType type = new PipeletType(nodes, edges, "anything", "anything");

        type.addForwardEntryPoint(n1, 0, "anything");
        type.addReverseEntryPoint(n2, 0, "anything");
        type.addExitPoint(n3, 0);

        PipeletInstance instance = new PipeletInstance(0, type);

        assertTrue(instance.place(servers));
        assertEquals(13.0, servers.get(0).availableCores(), 1e-15);

        instance.clearPlacement();
        assertEquals(16.0, servers.get(0).availableCores(), 1e-15);
    }

    @Test
    public void testPlace() throws Exception {
        List<Server> servers = new ArrayList<Server>();
        for (int i = 0; i < 10; ++i) {
            servers.add(new Server(16.0, 128.0));
        }

        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Vertex n3 = new Vertex("isonf", "n3");
        List<Vertex> nodes = new ArrayList<Vertex>();
        nodes.add(n1);
        nodes.add(n2);
        nodes.add(n3);

        Edge e1 = new Edge(n1, n3, 0, 0, "filter1");
        Edge e2 = new Edge(n2, n3, 0, 0, "filter2");
        List<Edge> edges = new ArrayList<Edge>();
        edges.add(e1);
        edges.add(e2);

        PipeletType type = new PipeletType(nodes, edges, "anything", "anything");

        type.addForwardEntryPoint(n1, 0, "anything");
        type.addReverseEntryPoint(n2, 0, "anything");
        type.addExitPoint(n3, 0);

        PipeletInstance instance = new PipeletInstance(0, type);

        assertTrue(instance.place(servers));
        assertEquals(13.0, servers.get(0).availableCores(), 1e-15);
    }
}