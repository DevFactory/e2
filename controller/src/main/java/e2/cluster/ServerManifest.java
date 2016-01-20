package e2.cluster;

public class ServerManifest {
    public double cores;
    public double memory;

    public String address;
    public int port;

    public ServerManifest(double cores, double memory, String address, int port) {
        this.cores = cores;
        this.memory = memory;
        this.address = address;
        this.port = port;
    }
}
