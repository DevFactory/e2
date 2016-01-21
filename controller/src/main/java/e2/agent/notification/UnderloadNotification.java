package e2.agent.notification;

import e2.agent.NotificationAgent;
import e2.cluster.Server;

public class UnderloadNotification extends BaseNotification {
    public int pipeletInstanceId;
    public int vertexInstanceId;

    public UnderloadNotification(NotificationAgent agent, Server server, int pipeletId, int vertexId) {
        this.agent = agent;
        this.source = server;
        this.type = NotificationType.UNDERLOAD;
        this.pipeletInstanceId = pipeletId;
        this.vertexInstanceId = vertexId;
    }
}
