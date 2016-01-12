package pipelet;

public final class Edge {
    private Vertex source = null;
    private Vertex target = null;
    private String filter = null;

    public Edge(Vertex source, Vertex target, String filter) {
        this.source = source;
        this.target = target;
        this.filter = filter;
    }

    public Edge(Vertex source, Vertex target) {
        this.source = source;
        this.target = target;
    }

    public String getFilter() {
        return filter;
    }
}
