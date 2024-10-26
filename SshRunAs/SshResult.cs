//
//          Copyright Seth Hendrick 2019-2024.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

namespace SshRunAs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ExitCode">
    /// Gets the number representing the exit status of the command, if applicable,
    /// otherwise <see langword="null"/>.
    /// The value is not <see langword="null"/> when an exit status code has been returned
    /// from the server. If the command terminated due to a signal, <paramref name="ExitSignal"/>
    /// may be not <see langword="null"/> instead.
    /// </param>
    /// <param name="ExitSignal">
    /// Gets the name of the signal due to which the command
    /// terminated violently, if applicable, otherwise <see langword="null"/>.
    /// The value (if it exists) is supplied by the server and is usually one of the
    /// following, as described in https://datatracker.ietf.org/doc/html/rfc4254#section-6.10:
    /// ABRT, ALRM, FPE, HUP, ILL, INT, KILL, PIPE, QUIT, SEGV, TER, USR1, USR2.
    /// </param>
    public record class SshResult(
        int? ExitCode,
        string ExitSignal
    );
}
