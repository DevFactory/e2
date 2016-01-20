package e2.cluster;

public class Switch {
    private String publicAddress;

    public Switch(String address) {
        publicAddress = address;
    }

    public boolean addServer() {
        return true;
    }

    public boolean removeServer() {
        return true;
    }

}
