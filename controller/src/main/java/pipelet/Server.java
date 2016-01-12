package pipelet;

public class Server {
    private double totalCores = 0.0;
    private double usedCores = 0.0;
    private double totalMemory = 0.0;
    private double usedMemory = 0.0;

    public Server(double cores, double memory) {
        totalCores = cores;
        totalMemory = memory;
    }

    public double availableCores() {
        return totalCores - usedCores;
    }

    public double availableMemory() {
        return totalMemory - usedMemory;
    }

    public boolean satisfy(double cores, double memory) {
        return (cores <= availableCores()) && (memory <= availableMemory());
    }

    public boolean consume(double cores, double memory) {
        if (satisfy(cores, memory)) {
            usedCores += cores;
            usedMemory += memory;
            return true;
        } else {
            return false;
        }
    }
}
