package e2;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ExecutionException;
import java.util.stream.Collectors;

import e2.agent.ServerAgentException;
import e2.agent.notification.BaseNotification;
import e2.agent.notification.ErrorNotification;
import e2.agent.notification.OverloadNotification;
import e2.agent.notification.UnderloadNotification;
import e2.cluster.Server;
import e2.cluster.ServerManifest;
import e2.pipelet.Edge;
import e2.pipelet.PipeletInstance;
import e2.pipelet.PipeletManager;
import e2.pipelet.PipeletType;
import e2.pipelet.Vertex;

public class Master {
    private static void printLogo() {
        System.out.println("      __         __  _              __       ");
        System.out.println(" ___ / /__ ____ / /_(_)___  ___ ___/ /__ ____ ");
        System.out.println("/ -_) / _ `(_-</ __/ / __/ / -_) _  / _ `/ -_)");
        System.out.println("\\__/_/\\_,_/___/\\__/_/\\__/  \\__/\\_,_/\\_, /\\__/ ");
        System.out.println("                                   /___/    ");
    }

    private static PipeletType makeTestPipeletType() {
        Vertex n1 = new Vertex("isonf", "n1");
        Vertex n2 = new Vertex("isonf", "n2");
        Vertex n3 = new Vertex("isonf", "n3");
        List<Vertex> nodes = new ArrayList<>();
        nodes.add(n1);
        nodes.add(n2);
        nodes.add(n3);

        Edge e1 = new Edge(n1, n3, 0, 0, "filter1");
        Edge e2 = new Edge(n2, n3, 0, 0, "filter2");
        List<Edge> edges = new ArrayList<>();
        edges.add(e1);
        edges.add(e2);

        PipeletType type = new PipeletType(nodes, edges, "anything", "anything");

        type.addForwardEntryPoint(n1, 0, "anything");
        type.addReverseEntryPoint(n2, 0, "anything");
        type.addExitPoint(n3, 0);

        return type;
    }

    PipeletManager manager;
    BlockingQueue<BaseNotification> notifications = new ArrayBlockingQueue<>(1024);

    public Master(String switchAddress) throws IOException, ServerAgentException, ExecutionException {
        manager = new PipeletManager(switchAddress);
        manager.addType(makeTestPipeletType());

        for (int i = 0; i < 1; ++i) {
            ServerManifest manifest = new ServerManifest(16.0, 128.0, "127.0.0.1", 10516);
            manager.addServer(new Server(manifest, notifications));
        }


    }

    public void run() throws Exception {
        List<PipeletInstance> instances = manager.getTypes()
                .stream()
                .map(PipeletInstance::new)
                .collect(Collectors.toList());

        for (PipeletInstance i : instances) {
            manager.addInstance(i);
        }

        while (true) {
            BaseNotification n = notifications.take();
            switch (n.type) {
                case OVERLOAD:
                    OverloadNotification overloadMsg = (OverloadNotification) n;
                    System.out.println("Pipelet " + overloadMsg.pipeletInstanceId + " on server " + overloadMsg.source + "is overloaded.");
                    break;
                case UNDERLOAD:
                    UnderloadNotification underloadMsg = (UnderloadNotification) n;
                    System.out.println("Pipelet " + underloadMsg.pipeletInstanceId + " on server " + underloadMsg.source + "is underloaded.");
                    break;
                case ERROR:
                    ErrorNotification errorMsg = (ErrorNotification) n;
                    System.out.println("Exception " + errorMsg.error.toString() + " from server " + errorMsg.source);
                    break;
                default:
                    System.out.println("Unrecognized notification type " + n.type);
            }
        }

    }

    public static void main(String[] args) throws Exception {
        printLogo();

        Master instance = new Master("192.168.0.1");
        instance.run();
    }
}
