package e2.pipelet;

public class PipeletInstance {
    private PipeletType type = null;
    private State state = State.INACTIVE;

    public PipeletInstance(PipeletType type) {
        this.type = type;
    }

    public PipeletType getType() {
        return type;
    }

    public String getName() {
        return "i" + Integer.toString(this.hashCode());
    }
}
