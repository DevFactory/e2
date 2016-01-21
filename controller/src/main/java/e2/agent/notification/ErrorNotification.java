package e2.agent.notification;

import e2.agent.NotificationAgent;
import e2.cluster.Server;

public class ErrorNotification extends BaseNotification {
    public Throwable error;

    public ErrorNotification(NotificationAgent agent, Server server, Throwable exc) {
        this.agent = agent;
        this.source = server;
        this.type = NotificationType.ERROR;
        this.error = exc;
    }
}
