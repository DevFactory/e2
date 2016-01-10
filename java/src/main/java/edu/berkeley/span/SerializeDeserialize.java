package edu.berkeley.span;
import com.google.protobuf.ExtensionRegistry;
import com.google.protobuf.InvalidProtocolBufferException;
import edu.berkeley.span.Request;
import edu.berkeley.span.Request.*;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.Request.Command.Builder;
import edu.berkeley.span.Resp;
import edu.berkeley.span.Resp.Response;
public final class SerializeDeserialize {
	private ExtensionRegistry _registry;
	public SerializeDeserialize() {
		_registry = ExtensionRegistry.newInstance();
		// Register the extensions so things get decoded correctly
		Request.registerAllExtensions(_registry);
		Resp.registerAllExtensions(_registry);
	}

	public Command ListRunning(String kind) {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.ListRunning);
		ListRunning.Builder abuild = ListRunning.newBuilder();
		if (kind != null) {
			abuild.setKind(kind);
		}
		cbuild.setExtension(ListRunning.args, abuild.build());
		return cbuild.build();
	}

	public Command ListRunning() {
		return ListRunning(null);
	}

	public Command ListMarked(String kind) {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.ListMarked);
		ListMarked.Builder abuild = ListMarked.newBuilder();
		if (kind != null) {
			abuild.setKind(kind);
		}
		cbuild.setExtension(ListMarked.args, abuild.build());
		return cbuild.build();
	}

	public Command ListMarked() {
		return ListRunning(null);
	}

	public Command StartBess() {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.StartBess);
		cbuild.setExtension(StartBess.args, StartBess.getDefaultInstance());
		return cbuild.build();
	}

	public Command StopBess() {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.StopBess);
		cbuild.setExtension(StopBess.args, StopBess.getDefaultInstance());
		return cbuild.build();
	}

	public Command MachineStatusString() {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.MachineStatusString);
		cbuild.setExtension(MachineStatusString.args, 
				MachineStatusString.getDefaultInstance());
		return cbuild.build();
	}

	public Command NewInstance(String type, String id) {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.NewInstance);
		cbuild.setExtension(NewInstance.args, 
				NewInstance.newBuilder()
				.setType(type)
				.setInstanceId(id).build());
		return cbuild.build();
	}

	public Command RegisterForNotification() {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.RegisterForNotification);
		cbuild.setExtension(RegisterForNotification.args,
				RegisterForNotification.getDefaultInstance());
		return cbuild.build();
	}

	public Command UnregisterFromNotification() {
		Command.Builder cbuild = Command.newBuilder();
		cbuild.setCommand(Command.Commands.UnregisterFromNotification);
		cbuild.setExtension(UnregisterFromNotification.args,
				UnregisterFromNotification.getDefaultInstance());
		return cbuild.build();
	}

	public Response ParseResponse(byte[] array, int len) throws InvalidProtocolBufferException {
		return Response.PARSER.parseFrom(array, 0, len, _registry); 
	}
}
