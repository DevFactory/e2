package pipelet;

import java.util.ArrayList;
import java.util.List;

public class PipeletType {
    private List<Vertex> nodes = new ArrayList<Vertex>();
    private List<Edge> edges = new ArrayList<Edge>();
    private List<Vertex> forwardEntryPoints = new ArrayList<Vertex>();
    private List<Vertex> reverseEntryPoints = new ArrayList<Vertex>();
    private List<Vertex> exitPoints = new ArrayList<Vertex>();

    public PipeletType(List<Vertex> nodes, List<Edge> edges) {
        this.nodes.addAll(nodes);
        this.edges.addAll(edges);
    }

    public List<Vertex> getNodes() {
        return new ArrayList<Vertex>(nodes);
    }

    public boolean addForwardEntryPoints(List<Vertex> points) {
        if (!nodes.containsAll(points)) {
            throw new RuntimeException("Vertex not found.");
        }
        return this.forwardEntryPoints.addAll(points);
    }

    public void clearForwardEntryPoints() {
        this.forwardEntryPoints.clear();
    }

    public boolean addReverseEntryPoints(List<Vertex> points) {
        if (!nodes.containsAll(points)) {
            throw new RuntimeException("Vertex not found.");
        }
        return this.reverseEntryPoints.addAll(points);
    }

    public void clearReverseEntryPoints() {
        this.reverseEntryPoints.clear();
    }

    public boolean addExitPoints(List<Vertex> points) {
        if (!nodes.containsAll(points)) {
            throw new RuntimeException("Vertex not found.");
        }
        return this.exitPoints.addAll(points);
    }

    public void clearExitPoints() {
        this.exitPoints.clear();
    }
}
