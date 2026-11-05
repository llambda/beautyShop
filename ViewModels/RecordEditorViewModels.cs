namespace BeautyStudio.ViewModels;

public sealed class InterventionEditorViewModel
{
    public CustomerDetailsViewModel Parent { get; }
    public InterventionEditorViewModel(CustomerDetailsViewModel parent) => Parent = parent;
}

public sealed class AppointmentEditorViewModel
{
    public CustomerDetailsViewModel Parent { get; }
    public AppointmentEditorViewModel(CustomerDetailsViewModel parent) => Parent = parent;
}
