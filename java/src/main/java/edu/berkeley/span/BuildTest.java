package edu.berkeley.span;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.SerializeDeserialize;
import edu.berkeley.span.ServerAgent;
import edu.berkeley.span.NotificationAgent;
import edu.berkeley.span.ServerAgentException;
import java.util.Arrays;
import java.io.IOException;
import java.util.concurrent.ExecutionException;

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
		s.TriggerNotification(NotificationAgent.NotificationType.Overload, "Scooby", "Doo");
		System.out.println(s.MachineStatus());
		//s.CreateInstance("test", "test1");
	}
}
