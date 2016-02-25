package e2;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ExecutionException;
import java.util.logging.Logger;
import java.util.stream.Collectors;

import e2.agent.ServerAgentException;
import e2.agent.notification.BaseNotification;
import e2.agent.notification.ErrorNotification;
import e2.agent.notification.OverloadNotification;
import e2.agent.notification.UnderloadNotification;
import e2.cluster.Server;
import e2.conf.Config;
import e2.pipelet.Edge;
import e2.pipelet.PipeletInstance;
import e2.pipelet.PipeletManager;
import e2.pipelet.PipeletType;
import e2.pipelet.Vertex;

public class Main {
    private static final Logger log = Logger.getLogger(Main.class.getName());
    BlockingQueue<BaseNotification> notifications = new ArrayBlockingQueue<>(1024);
    PipeletManager manager;
    // Config
    Config config;

    public Main() {
        log.info("Loading configurations...");
        config = new Config(true);
        log.info("Configurations loaded.");
    }

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

    public static void main(String[] args) {
        printLogo();

        //Options options = new Options();
        //options.addOption("config", true, "Config file");

        //CommandLineParser parser = new DefaultParser();
        //CommandLine line;
        //try {
        //    line = parser.parse(options, args);
        //} catch (ParseException exc) {
        //    log.log(Level.SEVERE, "Parsing failure. Error: {0}", exc.getMessage());
        //    return;
        //}

        //String configFile = line.getOptionValue("config");

        Main instance = new Main();
        try {
            instance.Init();
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }

        try {
            instance.run();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public void Init() throws IOException, ServerAgentException, ExecutionException {
        String swAddr = config.get("e2.switch");

        manager = new PipeletManager(swAddr);
        manager.addType(makeTestPipeletType());

        int numServer = Integer.parseInt(config.get("e2.server.count"));
        log.info("Configurations loaded.");
        log.info(String.format("HW switch IP address: %s. %d servers in total.", swAddr, numServer));

        for (int i = 0; i < numServer; ++i) {
            double cpu = Double.parseDouble(config.get("e2.server." + i + ".cpu"));
            double mem = Double.parseDouble(config.get("e2.server." + i + ".mem"));
            String ip = config.get("e2.server." + i + ".ip");
            int port = Integer.parseInt(config.get("e2.server." + i + ".port"));

            log.info(String.format("Adding server %s:%d with %.2f CPUs and %.2f GB Mem.",
                    ip, port, cpu, mem));

            manager.addServer(new Server(notifications,
                    cpu, mem, ip, port));
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
}
