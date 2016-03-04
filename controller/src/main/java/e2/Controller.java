package e2;

import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.List;
import java.util.Properties;
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
import e2.conf.Configuration;
import e2.pipelet.PipeletInstance;
import e2.pipelet.PipeletManager;
import e2.web.ControllerUIWebServer;

public class Controller {
    private static final Logger log = Logger.getLogger(Controller.class.getName());

    BlockingQueue<BaseNotification> notifications = new ArrayBlockingQueue<>(1024);
    PipeletManager manager;

    // Configuration
    Configuration configuration;

    // Web server
    ControllerUIWebServer webServer;

    public Controller(String configFile) {
        if (configFile == null) {
            log.info("No configuration specified. Loading the default configuration...");
            configuration = new Configuration(true);
        } else {
            try {
                InputStream input = new FileInputStream(configFile);
                Properties prop = new Properties();
                prop.load(input);
                log.info(String.format("Loading configuration: %s", configFile));
                configuration = new Configuration(prop);
            } catch (FileNotFoundException e) {
                log.info("Configuration not found. Loading the default configuration...");
                configuration = new Configuration(true);
            } catch (IOException e) {
                log.info("Error loading configuration. Loading the default configuration...");
                configuration = new Configuration(true);
            }
        }
        log.info("Configuration loaded.");

        log.info("Starting web server.");
        webServer = new ControllerUIWebServer(this, configuration);
        webServer.startWebServer();
    }

    private static void printLogo() {
        System.out.println("      __         __  _              __       ");
        System.out.println(" ___ / /__ ____ / /_(_)___  ___ ___/ /__ ____ ");
        System.out.println("/ -_) / _ `(_-</ __/ / __/ / -_) _  / _ `/ -_)");
        System.out.println("\\__/_/\\_,_/___/\\__/_/\\__/  \\__/\\_,_/\\_, /\\__/ ");
        System.out.println("                                   /___/    ");
    }

    private static String makeTestPolicy() {
        return "isonf fw\n" +
                "isonf ids\n" +
                "isonf nat\n" +
                "pipeline default {\n" +
                "  inf: fw[0]\n" +
                "  inr: nat[1]\n" +
                "  out: fw[0] nat[1]\n" +
                "  fw[1][\"dst port 80\"] -> ids[0]\n" +
                "  fw[1][\"!(dst port 80)\"] -> nat[0]\n" +
                "  ids[1] -> nat[0]\n" +
                "  nat[0][\"src port 80\"] -> ids[1]\n" +
                "  nat[0][\"!(src port 80)\"] -> fw[1]\n" +
                "  ids[0] -> fw[1]\n" +
                "}\n";
    }

    public static void main(String[] args) {
        if ((args.length % 2) == 1) {
            log.severe("Invalid number of arguments.");
            return;
        }

        String configFile = null;

        for (int i = 0; i < args.length; i += 2) {
            String option = args[i];
            String value = args[i + 1];
            switch (option) {
                case "--config":
                    configFile = value;
                    break;
                default:
                    log.severe(String.format("Unrecognized argument: %s", option));
                    return;
            }
        }

        printLogo();

        Controller instance = new Controller(configFile);

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
        String swAddr = configuration.get(Constants.SWITCH_HOSTNAME);
        manager = new PipeletManager(swAddr);

        log.info("Parsing policy...");
        manager.parsePolicy(makeTestPolicy());

        int numServer = Integer.parseInt(configuration.get(Constants.SERVER_COUNT));
        log.info(String.format("HW switch IP address: %s. %d servers in total.",
                swAddr, numServer));

        for (int i = 0; i < numServer; ++i) {
            double cpu = Double.parseDouble(configuration.get("e2.server." + i + ".cpu"));
            double mem = Double.parseDouble(configuration.get("e2.server." + i + ".mem"));
            String ip = configuration.get("e2.server." + i + ".ip");
            int port = Integer.parseInt(configuration.get("e2.server." + i + ".port"));

            log.info(String.format("Adding server %s:%d with %.2f CPUs and %.2f GB Mem.",
                    ip, port, cpu, mem));

            manager.addServer(new Server(notifications,
                    cpu, mem, ip, port));
        }

        log.info("Init finished.");
    }

    public void run() throws Exception {
        log.info("Creating an instance for each pipelet type.");
        List<PipeletInstance> instances = manager.getTypes()
                .stream()
                .map(PipeletInstance::new)
                .collect(Collectors.toList());

        for (PipeletInstance i : instances) {
            manager.addInstance(i);
        }

        log.info("Listening to events.");
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
