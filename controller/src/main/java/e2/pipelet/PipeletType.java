package e2.pipelet;

import java.util.ArrayList;
import java.util.List;

public class PipeletType {
    private List<Vertex> nodes = new ArrayList<>();
    private Vertex forwardEntryNode = new Vertex("INF", "INF");
    private Vertex reverseEntryNode = new Vertex("INR", "INR");
    private Vertex exitNode = new Vertex("OUT", "OUT");

    private List<Edge> edges = new ArrayList<>();
    private List<Edge> virtualEdges = new ArrayList<>();

    private String externalFilter = null;
    private String name = null;

    public PipeletType(String name, List<Vertex> nodes, List<Edge> edges, String extFilter) {
        this.nodes.addAll(nodes);
        this.edges.addAll(edges);
        this.externalFilter = extFilter;
        this.name = name;
    }

    public String getName() {
        return name;
    }

    public List<Vertex> getRealNodes() {
        return new ArrayList<>(nodes);
    }

    public List<Vertex> getNodes() {
        List<Vertex> result = new ArrayList<>(nodes);
        result.add(forwardEntryNode);
        result.add(reverseEntryNode);
        result.add(exitNode);
        return result;
    }

    public List<Edge> getRealEdges() {
        return new ArrayList<>(edges);
    }

    public List<Edge> getEdges() {
        List<Edge> result = new ArrayList<>(edges);
        result.addAll(virtualEdges);
        return result;
    }

    public boolean addForwardEntryPoint(Vertex point, int port, String filter) {
        if (!nodes.contains(point)) {
            throw new RuntimeException("Vertex not found.");
        }
        return virtualEdges.add(new Edge(forwardEntryNode, point, -1, port, filter));
    }

    public boolean addReverseEntryPoint(Vertex point, int port, String filter) {
        if (!nodes.contains(point)) {
            throw new RuntimeException("Vertex not found.");
        }
        return virtualEdges.add(new Edge(reverseEntryNode, point, -1, port, filter));
    }

    public boolean addExitPoint(Vertex point, int port) {
        if (!nodes.contains(point)) {
            throw new RuntimeException("Vertex not found.");
        }
        return virtualEdges.add(new Edge(point, exitNode, port, -1, ""));
    }

    public String getExternalFilter() {
        return externalFilter;
    }
}
