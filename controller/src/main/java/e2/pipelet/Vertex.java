package e2.pipelet;

public final class Vertex {
    private String type = null;
    private String name = null;

    public Vertex(String type, String name) {
        this.type = type;
        this.name = name;
    }

    public String getType() {
        return type;
    }

    public String getName() {
        return name;
    }

    public double requiredCores() {
        return 1.0;
    }

    public double requiredMemory() {
        return 0.0;
    }
}
