package e2.agent.notification;

import e2.agent.NotificationAgent;
import e2.cluster.Server;

public class OverloadNotification extends BaseNotification {
    public int pipeletInstanceId;
    public int vertexInstanceId;

    public OverloadNotification(NotificationAgent agent, Server server, int pipeletId, int vertexId) {
        this.agent = agent;
        this.source = server;
        this.type = NotificationType.OVERLOAD;
        this.pipeletInstanceId = pipeletId;
        this.vertexInstanceId = vertexId;
    }
}
