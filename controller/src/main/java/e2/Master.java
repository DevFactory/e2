package e2;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ExecutionException;

import e2.agent.ServerAgentException;
import e2.cluster.Server;
import e2.cluster.ServerManifest;
import e2.pipelet.Edge;
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

    public static void run() throws IOException, ExecutionException, ServerAgentException {
        PipeletManager manager = new PipeletManager("192.168.0.1");

        manager.addType(makeTestPipeletType());

        for (int i = 0; i < 1; ++i) {
            ServerManifest manifest = new ServerManifest(16.0, 128.0, "127.0.0.1", 10516);
            manager.addServer(new Server(manifest));
        }

    }

    public static void main(String[] args) throws IOException, ExecutionException, ServerAgentException {
        printLogo();
        run();
    }
}
