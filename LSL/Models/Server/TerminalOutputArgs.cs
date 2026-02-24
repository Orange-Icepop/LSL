namespace LSL.Models.Server;

public record TerminalOutputArgs(int ServerId, string Output, OutputChannelType ChannelType); // 终端输出事件