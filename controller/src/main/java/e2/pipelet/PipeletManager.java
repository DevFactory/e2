package e2.pipelet;

import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.logging.Logger;

import e2.agent.ServerAgentException;
import e2.cluster.Server;
import e2.cluster.Switch;
import e2.parser.PolicyCompiler;

public class PipeletManager {
    private static final Logger log = Logger.getLogger(PipeletManager.class.getName());

    private List<PipeletType> types = new ArrayList<>();
    private List<PipeletInstance> instances = new ArrayList<>();
    private List<Server> servers = new ArrayList<>();
    private Switch hardwareSwitch = null;

    private Map<PipeletInstance, Server> placement = new HashMap<>();

    public PipeletManager(String switchAddress) {
        hardwareSwitch = new Switch(switchAddress);
    }

    public void addType(PipeletType type) throws IOException, ServerAgentException {
        for (Server server : servers) {
            server.addPipeletType(type);
        }
        types.add(type);
    }

    public void parsePolicy(String policy) throws IOException, ServerAgentException {
        List<PipeletType> types = PolicyCompiler.compile(policy);
        for (PipeletType type : types) {
            addType(type);
        }
    }

    public List<PipeletType> getTypes() {
        return types;
    }

    public void addServer(Server server) throws IOException, ServerAgentException {
        //log.info(String.format("Starting BESS on %s.", server.IP()));
        //server.startBess();

        log.info(String.format("Adding pipelet types on %s.", server.IP()));
        for (PipeletType type : types) {
            server.addPipeletType(type);
        }

        log.info(String.format("Configuring switch for %s.", server.IP()));
        hardwareSwitch.addServer();

        servers.add(server);
    }

    public void removeServer(Server server) throws IOException, ServerAgentException {
        server.stopBess();
        hardwareSwitch.removeServer();
        servers.remove(server);
    }

    protected void addInstanceInternal(PipeletInstance instance, Server destination) throws Exception {
        PipeletType type = instance.getType();
        double requiredCores = type.getRealNodes()
                .stream()
                .mapToDouble(Vertex::requiredCores)
                .sum();
        double requiredMemory = type.getRealNodes()
                .stream()
                .mapToDouble(Vertex::requiredMemory)
                .sum();


        if (destination == null) {
            destination = servers.stream()
                    .filter(server -> server.satisfy(requiredCores, requiredMemory))
                    .findFirst()
                    .orElseThrow(() -> new Exception("No available server for instance " + instance));
        }
        log.info(String.format("Running instance %s on Server %s.", instance.getName(), destination.IP()));
        destination.runPipeletInstance(instance);

        for (Server server: servers) {
            // Inform all other servers
            if (server != destination) {
        	    server.notifyRemotePipeletInstance(instance, destination);
	        }
	    }
        destination.consume(requiredCores, requiredMemory);
        placement.put(instance, destination);
        instances.add(instance);
    }

    public void addInstance(PipeletInstance instance) throws Exception {
        addInstanceInternal(instance, null);
    }

    // Bypass placement for testing, demoing or other reasons.
    public void addInstance(PipeletInstance instance, int server) throws Exception {
        if (server > servers.size()) {
            log.info("Cowardly refusing to launch on non-existent server");
            addInstance(instance);
        }
        Server s = servers.get(server);
        addInstanceInternal(instance, s);
    }

    public void removeInstance(PipeletInstance instance) throws IOException, ServerAgentException {
        double requiredCores = instance.getType().getRealNodes()
                .stream()
                .mapToDouble(Vertex::requiredCores)
                .sum();
        double requiredMemory = instance.getType().getRealNodes()
                .stream()
                .mapToDouble(Vertex::requiredMemory)
                .sum();

        Server server = placement.get(instance);
        server.stopPipeletInstance(instance);

        server.free(requiredCores, requiredMemory);
        placement.remove(instance);
        instances.remove(instance);
    }

    public void removeInstance(String instanceName) throws IOException, ServerAgentException {
        PipeletInstance instance = instances
                .stream()
                .filter(i -> i.getName().equals(instanceName))
                .findAny()
                .orElseThrow(() -> new RuntimeException("Removing an instance that has not been added."));
        removeInstance(instance);
    }

    public PipeletInstance findInstanceByName(String instanceName) {
        return instances.stream()
                .filter(i -> i.getName().equals(instanceName))
                .findAny()
                .orElseThrow(() -> new RuntimeException("Instance not found."));
    }
}
