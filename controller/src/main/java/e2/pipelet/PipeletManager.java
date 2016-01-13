package e2.pipelet;

import java.util.ArrayList;
import java.util.List;

public class PipeletManager {
    private static PipeletManager instance = null;

    private List<PipeletType> types = new ArrayList<PipeletType>();
    private List<PipeletInstance> instances = new ArrayList<PipeletInstance>();
    private List<Server> servers = new ArrayList<Server>();

    public boolean addType(PipeletType type) {
        return types.add(type);
    }

    public boolean addInstance(PipeletInstance instance) {
        return instances.add(instance);
    }

    public boolean removeInstance(PipeletInstance instance) {
        instance.clearPlacement();
        return instances.remove(instance);
    }

    public boolean removeInstance(int id) {
        PipeletInstance instance = null;
        for (PipeletInstance i : instances) {
            if (i.getId() == id) {
                instance = i;
                break;
            }
        }
        return (instance != null) && removeInstance(instance);
    }

    public boolean placeInstance(PipeletInstance instance) throws Exception {
        return instance.place(servers);
    }

    public boolean addServer(Server server) {
        return servers.add(server);
    }

}
