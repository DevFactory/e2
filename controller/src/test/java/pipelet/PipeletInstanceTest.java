package pipelet;

import org.junit.Test;

import java.util.ArrayList;
import java.util.List;

import static org.junit.Assert.*;

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

        Edge e1 = new Edge(n1, n3);
        Edge e2 = new Edge(n2, n3);
        List<Edge> edges = new ArrayList<Edge>();
        edges.add(e1);
        edges.add(e2);

        PipeletType type = new PipeletType(nodes, edges);

        List<Vertex> fwd = new ArrayList<Vertex>();
        fwd.add(n1);
        type.addForwardEntryPoints(fwd);

        List<Vertex> rev = new ArrayList<Vertex>();
        rev.add(n2);
        type.addReverseEntryPoints(rev);

        List<Vertex> exit = new ArrayList<Vertex>();
        exit.add(n3);
        type.addExitPoints(exit);

        PipeletInstance instance = new PipeletInstance(0, type);

        assertTrue(instance.place(servers));
        assertEquals(servers.get(0).availableCores(), 13.0, 1e-15);

        instance.clearPlacement();
        assertEquals(servers.get(0).availableCores(), 16.0, 1e-15);
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

        Edge e1 = new Edge(n1, n3);
        Edge e2 = new Edge(n2, n3);
        List<Edge> edges = new ArrayList<Edge>();
        edges.add(e1);
        edges.add(e2);

        PipeletType type = new PipeletType(nodes, edges);

        List<Vertex> fwd = new ArrayList<Vertex>();
        fwd.add(n1);
        type.addForwardEntryPoints(fwd);

        List<Vertex> rev = new ArrayList<Vertex>();
        rev.add(n2);
        type.addReverseEntryPoints(rev);

        List<Vertex> exit = new ArrayList<Vertex>();
        exit.add(n3);
        type.addExitPoints(exit);

        PipeletInstance instance = new PipeletInstance(0, type);

        assertTrue(instance.place(servers));
        assertEquals(servers.get(0).availableCores(), 13.0, 1e-15);
    }
}