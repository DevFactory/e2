package e2.cluster;

import java.io.IOException;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ExecutionException;
import java.util.logging.Logger;

import e2.agent.NotificationAgent;
import e2.agent.ServerAgent;
import e2.agent.ServerAgentException;
import e2.agent.notification.BaseNotification;
import e2.agent.notification.ErrorNotification;
import e2.agent.notification.OverloadNotification;
import e2.agent.notification.UnderloadNotification;
import e2.pipelet.PipeletInstance;
import e2.pipelet.PipeletType;

public class Server {
    private static final Logger log = Logger.getLogger(Server.class.getName());

    private Resource totalResources;
    private Resource usedResources;
    private ServerAgent agent;
    private NotificationAgent notification;
    private String address;

    public Server(BlockingQueue<BaseNotification> notifications,
                  double core,
                  double mem,
                  String ip,
                  int port) throws IOException, ExecutionException, ServerAgentException {
        totalResources = new Resource(core, mem);
        usedResources = new Resource(0.0, 0.0);
        address = ip;

        log.info(String.format("Connecting to %s:%d.", ip, port));
        agent = new ServerAgent(ip, port);

        log.info(String.format("Preparing to listen on %s:%d for events.", ip, port));
        notification = new NotificationAgent(ip, port);

        notification.AwaitNotification(
                // NotificationCallback
                (NotificationAgent nAgent, NotificationAgent.NotificationType type, String p, String nf) -> {
                    BaseNotification notification;
                    if (type == NotificationAgent.NotificationType.Overload) {
                        notification = new OverloadNotification(nAgent, this, Integer.parseInt(p), Integer.parseInt(nf));
                    } else {
                        notification = new UnderloadNotification(nAgent, this, Integer.parseInt(p), Integer.parseInt(nf));
                    }
                    try {
                        notifications.put(notification);
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                    }
                },
                // FailureNotification
                (NotificationAgent nAgent, Throwable err) -> {
                    try {
                        notifications.put(new ErrorNotification(nAgent, this, err));
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                    }
                }
        );

        log.info(String.format("Listening on %s:%d for events.", ip, port));
    }

    public double availableCores() {
        return totalResources.core - usedResources.core;
    }

    public double availableMemory() {
        return totalResources.memory - usedResources.memory;
    }

    public String IP() {
        return address;
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

    public void startBess() throws IOException, ServerAgentException {
        agent.StartBess();
    }

    public void stopBess() throws IOException, ServerAgentException {
        agent.StopBess();
    }

    public void addPipeletType(PipeletType type) throws IOException, ServerAgentException {
        String typeId = type.getName();
        agent.NewPipelet(typeId, type);
    }

    public void runPipeletInstance(PipeletInstance instance) throws IOException, ServerAgentException {
        String typeId = instance.getType().getName();
        String instanceId = "i" + Integer.toString(instance.hashCode());
        agent.CreateInstance(typeId, instanceId);
    }

    public void stopPipeletInstance(PipeletInstance instance) throws IOException, ServerAgentException {
        String instanceId = "i" + Integer.toString(instance.hashCode());
        agent.DestroyInstance(instanceId);
    }
}
