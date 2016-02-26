package e2.agent;


import com.google.protobuf.ExtensionRegistry;
import com.google.protobuf.InvalidProtocolBufferException;

import java.util.List;

import e2.pipelet.Edge;
import e2.pipelet.Vertex;
import e2.proto.agent.Notification.Upcall;
import e2.proto.agent.Request;
import e2.proto.agent.Request.Command;
import e2.proto.agent.Request.KillInstance;
import e2.proto.agent.Request.Link;
import e2.proto.agent.Request.ListMarked;
import e2.proto.agent.Request.ListRunning;
import e2.proto.agent.Request.MachineStatusString;
import e2.proto.agent.Request.NewInstance;
import e2.proto.agent.Request.NewPipelet;
import e2.proto.agent.Request.RegisterForNotification;
import e2.proto.agent.Request.StartBess;
import e2.proto.agent.Request.StopBess;
import e2.proto.agent.Request.TriggerNotification;
import e2.proto.agent.Request.UnregisterFromNotification;
import e2.proto.agent.Resp;
import e2.proto.agent.Resp.Response;

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

    public Command KillInstance(String id) {
        Command.Builder cbuild = Command.newBuilder();
        cbuild.setCommand(Command.Commands.KillInstance);
        cbuild.setExtension(KillInstance.args,
                KillInstance.newBuilder()
                        .setInstanceId(id).build());
        return cbuild.build();
    }

    public Command NewPipelet(String type, List<Vertex> nfs, List<Edge> connections, String externalFilter) {
        Command.Builder cbuild = Command.newBuilder();
        cbuild.setCommand(Command.Commands.NewPipelet);
        NewPipelet.Builder abuild = NewPipelet.newBuilder();
        abuild.setType(type);
        for (Vertex n : nfs) {
            if (n.getType().equals("INF") || n.getType().equals("INR") || n.getType().equals("OUT")) {
                continue;
            }
            abuild.addNfs(NewPipelet.NF.newBuilder()
                    .setId(n.getName())
                    .setType(n.getType())
                    .build());
        }
        for (Edge e : connections) {
            Link.Endpoint.Builder b1 = Link.Endpoint.newBuilder().setNf(e.getSource().getName());
            if (e.getSourcePort() != -1) {
                b1.setVport(e.getSourcePort());
            }

            Link.Endpoint.Builder b2 = Link.Endpoint.newBuilder().setNf(e.getTarget().getName());
            if (e.getTargetPort() != -1) {
                b2.setVport(e.getTargetPort());
            }

            Link link = Link.newBuilder()
                    .setSrc(b1.build())
                    .setDst(b2.build())
                    .build();

            abuild.addConnections(link);

            abuild.addInternalFilters(NewPipelet.InternalFilter.newBuilder()
                    .setLink(link)
                    .setFilter(e.getFilter()));
        }

        abuild.setExternalFilter(externalFilter);
        cbuild.setExtension(NewPipelet.args, abuild.build());

        return cbuild.build();
    }

    public Command TriggerNotification(NotificationAgent.NotificationType type,
                                       String pipeletId,
                                       String nfId) {
        TriggerNotification.Type pbType;
        if (type == NotificationAgent.NotificationType.Overload) {
            pbType = TriggerNotification.Type.Overload;
        } else {
            pbType = TriggerNotification.Type.Underload;
        }
        Command.Builder cbuild = Command.newBuilder();
        cbuild.setCommand(Command.Commands.TriggerNotification);
        cbuild.setExtension(TriggerNotification.args,
                TriggerNotification.newBuilder()
                        .setType(pbType)
                        .setPipeletId(pipeletId)
                        .setNfId(nfId).build());
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

    public Upcall ParseNotification(byte[] array, int len) throws InvalidProtocolBufferException {
        return Upcall.PARSER.parseFrom(array, 0, len, _registry);
    }
}
