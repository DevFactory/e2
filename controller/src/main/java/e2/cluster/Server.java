package e2.cluster;

import java.io.IOException;
import java.util.concurrent.ExecutionException;

import e2.agent.NotificationAgent;
import e2.agent.ServerAgent;
import e2.agent.ServerAgentException;
import e2.pipelet.PipeletInstance;
import e2.pipelet.PipeletType;

public class Server {
    private Resource totalResources;
    private Resource usedResources;
    private ServerAgent agent;
    private NotificationAgent notification;

    public Server(ServerManifest manifest) throws IOException, ExecutionException {
        totalResources = new Resource(manifest.cores, manifest.memory);
        usedResources = new Resource(0.0, 0.0);
        agent = new ServerAgent(manifest.address, manifest.port);
        notification = new NotificationAgent(manifest.address, manifest.port);
    }

    public double availableCores() {
        return totalResources.core - usedResources.core;
    }

    public double availableMemory() {
        return totalResources.memory - usedResources.memory;
    }

    public boolean satisfy(double cores, double memory) {
        return (cores <= availableCores()) && (memory <= availableMemory());
    }

    public boolean consume(double cores, double memory) {
        if (satisfy(cores, memory)) {
            usedResources.core += cores;
            usedResources.memory += memory;
            return true;
        } else {
            return false;
        }
    }

    public void free(double cores, double memory) {
        usedResources.core -= cores;
        usedResources.memory -= memory;
    }

    public void addPipeletType(PipeletType type) throws IOException, ServerAgentException {
        String typeId = Integer.toString(type.hashCode());
        agent.NewPipelet(typeId, type);
    }

    public void runPipeletInstance(PipeletInstance instance) throws IOException, ServerAgentException {
        String typeId = Integer.toString(instance.getType().hashCode());
        String instanceId = Integer.toString(instance.hashCode());
        agent.CreateInstance(typeId, instanceId);
    }

    public void stopPipeletInstance(PipeletInstance instance) throws IOException, ServerAgentException {
        String instanceId = Integer.toString(instance.hashCode());
        agent.DestroyInstance(instanceId);
    }
}
