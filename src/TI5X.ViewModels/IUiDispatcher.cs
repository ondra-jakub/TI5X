namespace TI5X.ViewModels;

public interface IUiDispatcher
{
    void Post(Action action);
}
