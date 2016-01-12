package pipelet;

import java.util.ArrayList;
import java.util.List;

public class PipeletManager {
    private static PipeletManager instance = null;

    private List<PipeletType> types = new ArrayList<PipeletType>();
    private List<PipeletInstance> instances = new ArrayList<PipeletInstance>();
    private List<Server> servers = new ArrayList<Server>();

    protected PipeletManager() {
        // Singleton
    }

    public static PipeletManager getInstance() {
        if (instance == null) {
            // Not thread-safe
            instance = new PipeletManager();
        }
        return instance;
    }

    public boolean AddType(PipeletType type) {
        return types.add(type);
    }

    public boolean AddInstance(PipeletInstance instance) {
        return instances.add(instance);
    }

    public boolean AddServer(Server server) {
        return servers.add(server);
    }

}
