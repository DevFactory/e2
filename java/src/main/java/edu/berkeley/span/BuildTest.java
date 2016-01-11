package edu.berkeley.span;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.SerializeDeserialize;
import edu.berkeley.span.ServerAgent;
import edu.berkeley.span.NotificationAgent;
import edu.berkeley.span.ServerAgentException;
import edu.berkeley.span.Edge;
import edu.berkeley.span.Vertex;
import java.util.Arrays;
import java.io.IOException;
import java.util.concurrent.ExecutionException;
import java.util.HashMap;
import java.util.List;
import java.util.ArrayList;

public class BuildTest {
	public static void main(String[] args) throws IOException, 
	       ServerAgentException, ExecutionException {
		ServerAgent s = new ServerAgent();
		System.out.println("Hello from java land");
		System.out.println(s.MachineStatus());
		NotificationAgent n = new NotificationAgent();
		n.AwaitNotification(
			(agent, type, pipelet, nf) -> {
				System.out.println("Received notification of type " + type + " for " + pipelet + ", "
						+ nf);
			},
			(agent, exc) -> {System.out.println("Exception " + exc.toString());});
		HashMap<String, String> nfs = new HashMap<String, String>();
		nfs.put("n1", "isonf");
		nfs.put("n2", "isonf");
		nfs.put("n3", "isonf");
		nfs.put("n4", "isonf");

		List<Edge> connections = new ArrayList<Edge>(6);
		connections.add(new Edge(Vertex.ForwardIn(), new Vertex("n1", 0)));
		connections.add(new Edge(new Vertex("n1", 1), new Vertex("n2", 0)));
		connections.add(new Edge(new Vertex("n2", 1), new Vertex("n3", 0)));
		connections.add(new Edge(new Vertex("n2", 1), new Vertex("n4", 0)));
		connections.add(new Edge(new Vertex("n3", 1), Vertex.Out()));
		connections.add(new Edge(new Vertex("n4", 1), Vertex.Out()));

		HashMap<Edge, String> ifilter = new HashMap<Edge, String>();
		ifilter.put(new Edge(new Vertex("n2", 1), new Vertex("n4", 0)), "udp");

		s.NewPipelet("test", nfs, connections, ifilter, "");
		s.CreateInstance("test", "test0");
		s.TriggerNotification(NotificationAgent.NotificationType.Overload, "Scooby", "Doo");
		System.out.println(s.MachineStatus());
		s.DestroyInstance("test0");
	}
}
