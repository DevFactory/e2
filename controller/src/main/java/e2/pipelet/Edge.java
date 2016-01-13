package e2.pipelet;

public final class Edge {
    private Vertex source = null;
    private Vertex target = null;
    private int sourcePort = -1;
    private int targetPort = -1;
    private String filter = null;

    public Edge(Vertex source, Vertex target, int sourcePort, int targetPort, String filter) {
        this.source = source;
        this.target = target;
        this.sourcePort = sourcePort;
        this.targetPort = targetPort;
        this.filter = filter;
    }

    public String getFilter() {
        return filter;
    }

    public Vertex getSource() {
        return source;
    }

    public Vertex getTarget() {
        return target;
    }

    public int getSourcePort() {
        return sourcePort;
    }

    public int getTargetPort() {
        return targetPort;
    }
}
