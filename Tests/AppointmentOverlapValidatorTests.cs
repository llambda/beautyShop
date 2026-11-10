using BeautyStudio.Models;
using BeautyStudio.Services;
using System;
using Xunit;

namespace BeautyStudio.Tests;

public sealed class AppointmentOverlapValidatorTests
{
    private static readonly DateTime Start = new(2026, 11, 9, 10, 0, 0);

    [Fact]
    public void DetectsPartialOverlap()
    {
        var existing = new Appointment { Date = Start.AddMinutes(45), DurationMinutes = 60 };
        Assert.True(AppointmentOverlapValidator.HasOverlap(Start, 60, new[] { existing }));
    }

    [Fact]
    public void AllowsAppointmentsThatTouchAtTheBoundary()
    {
        var existing = new Appointment { Date = Start.AddMinutes(60), DurationMinutes = 60 };
        Assert.False(AppointmentOverlapValidator.HasOverlap(Start, 60, new[] { existing }));
    }

    [Theory]
    [InlineData("Annullato")]
    [InlineData("Non presentato")]
    public void IgnoresInactiveAppointments(string status)
    {
        var existing = new Appointment { Date = Start.AddMinutes(15), DurationMinutes = 60, Status = status };
        Assert.False(AppointmentOverlapValidator.HasOverlap(Start, 60, new[] { existing }));
    }

    [Fact]
    public void DetectsAppointmentContainingRequestedTime()
    {
        var existing = new Appointment { Date = Start.AddMinutes(-30), DurationMinutes = 90 };
        Assert.True(AppointmentOverlapValidator.HasOverlap(Start, 30, new[] { existing }));
    }

    [Fact]
    public void RejectsInvalidRequestedDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AppointmentOverlapValidator.HasOverlap(Start, 0, Array.Empty<Appointment>()));
    }

    [Fact]
    public void AllowsExactlyTwelveHours()
    {
        Assert.False(AppointmentOverlapValidator.HasOverlap(Start, 720, Array.Empty<Appointment>()));
    }

    [Fact]
    public void RejectsDurationLongerThanTwelveHours()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AppointmentOverlapValidator.HasOverlap(Start, 721, Array.Empty<Appointment>()));
    }

    [Fact]
    public void IgnoresInactiveStatusRegardlessOfLetterCase()
    {
        var existing = new Appointment { Date = Start.AddMinutes(10), DurationMinutes = 60, Status = "annullato" };
        Assert.False(AppointmentOverlapValidator.HasOverlap(Start, 60, new[] { existing }));
    }

    [Fact]
    public void IgnoresInvalidLegacyDuration()
    {
        var existing = new Appointment { Date = Start.AddMinutes(10), DurationMinutes = 0 };
        Assert.False(AppointmentOverlapValidator.HasOverlap(Start, 60, new[] { existing }));
    }

}
