package e2.pipelet;

import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

public class PipeletInstance {

    private PipeletType type = null;
    private State state = State.INACTIVE;
    private int id = -1;
    private HashMap<Vertex, Server> placement = new HashMap<Vertex, Server>();

    public PipeletInstance(int id, PipeletType type) {
        this.id = id;
        this.type = type;
    }

    public int getId() {
        return id;
    }

    public void clearPlacement() {
        for (Map.Entry<Vertex, Server> vertexServerEntry : placement.entrySet()) {
            Server server = vertexServerEntry.getValue();
            Vertex node = vertexServerEntry.getKey();
            server.free(node.requiredCores(), node.requiredMemory());
        }

        placement.clear();
    }

    private boolean isFeasiblePlacement(List<Server> servers) {
        double totalAvailableCores = 0.0, totalAvailableMemory = 0.0;
        double totalRequiredCores = 0.0, totalRequiredMemory = 0.0;
        List<Vertex> nodes = type.getRealNodes();
        for (Server s : servers) {
            totalAvailableCores += s.availableCores();
            totalAvailableMemory += s.availableMemory();
        }
        for (Vertex node : nodes) {
            totalRequiredCores += node.requiredCores();
            totalRequiredMemory += node.requiredMemory();
        }

        return (totalRequiredCores <= totalAvailableCores) && (totalRequiredMemory <= totalAvailableMemory);
    }

    public boolean place(List<Server> servers) throws Exception {
        boolean feasible = isFeasiblePlacement(servers);
        if (!feasible) {
            return false;
        }
        List<Vertex> nodes = type.getRealNodes();
        Iterator<Server> s = servers.iterator();
        Server server = s.next();

        for (Vertex node : nodes) {
            while (!server.satisfy(node.requiredCores(), node.requiredMemory()) && s.hasNext()) {
                server = s.next();
            }

            if (!s.hasNext()) {
                throw new Exception("No feasible placement for Vertex: " + node + ".");
            }

            server.consume(node.requiredCores(), node.requiredMemory());
            placement.put(node, server);
        }
        return true;
    }
}
