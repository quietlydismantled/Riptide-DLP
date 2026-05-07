namespace YtDlpGui.Core.Models;

public enum DlStatus { Queued, Downloading, Complete, Error, Cancelled }

public record struct OutputRecord(string Line, bool IsError);
