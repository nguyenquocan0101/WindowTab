#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <ocidl.h>
#include <xamlom.h>
#undef GetCurrentTime
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Xaml.Automation.Peers.h>
#include <winrt/Windows.UI.Xaml.Media.h>
#include <winrt/Windows.UI.Xaml.Shapes.h>
#include <algorithm>
#include <cmath>
#include <map>
#include <string>
#include <thread>

namespace
{
    namespace xaml = winrt::Windows::UI::Xaml;
    constexpr CLSID siteClass{ 0xd2568989, 0xb623, 0x475e, { 0xa3, 0x90, 0x29, 0x1b, 0x4d, 0x6c, 0x24, 0x90 } };
    constexpr auto enabledEventName = LR"(Local\WindowTab.TaskbarTransparency.Enabled.2)";
    constexpr auto appliedEventName = LR"(Local\WindowTab.TaskbarTransparency.Applied)";

    std::wstring AttachmentName(DWORD pid)
    {
        return LR"(Local\WindowTab.TaskbarTransparency.Explorer.)" + std::to_wstring(pid);
    }

    struct Background
    {
        winrt::weak_ref<xaml::Shapes::Rectangle> element;
        winrt::Windows::Foundation::IInspectable original{ nullptr };
        bool hidden = false;

        void Update(bool enabled)
        {
            if (hidden == enabled)
                return;

            const auto shape = element.get();
            if (!shape)
                return;

            const auto property = xaml::UIElement::OpacityProperty();
            if (enabled)
            {
                original = shape.ReadLocalValue(property);
                shape.Opacity(0);
            }
            else
            {
                if (shape.Opacity() == 0)
                {
                    if (original == xaml::DependencyProperty::UnsetValue())
                        shape.ClearValue(property);
                    else
                        shape.SetValue(property, original);
                }
                original = nullptr;
            }
            hidden = enabled;
        }
    };

    // Diagnostics callbacks and the timer both run on the element's XAML thread.
    thread_local std::map<InstanceHandle, Background> backgrounds;
    thread_local UINT_PTR timerId = 0;

    namespace peers = xaml::Automation::Peers;

    struct DockButton
    {
        winrt::weak_ref<xaml::FrameworkElement> element;
        xaml::Media::TransformGroup transform{ nullptr };
        xaml::Media::ScaleTransform scale{ nullptr };
        winrt::Windows::Foundation::IInspectable original{ nullptr };
        double current = 1;

        void Update(xaml::FrameworkElement const& button, double target)
        {
            current += (target - current) * 0.25;
            if (std::abs(target - current) < 0.001)
                current = target;

            const auto property = xaml::UIElement::RenderTransformProperty();
            if (current == 1 && target == 1)
            {
                if (transform && button.RenderTransform() == transform)
                {
                    if (original == xaml::DependencyProperty::UnsetValue())
                        button.ClearValue(property);
                    else
                        button.SetValue(property, original);
                }
                transform = nullptr;
                scale = nullptr;
                original = nullptr;
                return;
            }

            if (!transform || button.RenderTransform() != transform)
            {
                original = button.ReadLocalValue(property);
                const auto previous = button.RenderTransform();
                transform = xaml::Media::TransformGroup();
                scale = xaml::Media::ScaleTransform();
                if (previous)
                    transform.Children().Append(previous);
                transform.Children().Append(scale);
                button.RenderTransform(transform);
            }

            const auto origin = button.RenderTransformOrigin();
            scale.CenterX((0.5 - origin.X) * button.ActualWidth());
            scale.CenterY((0.5 - origin.Y) * button.ActualHeight());
            scale.ScaleX(current);
            scale.ScaleY(current);
        }
    };

    thread_local std::map<InstanceHandle, DockButton> dockButtons;
    thread_local UINT_PTR dockTimer = 0;
    thread_local int dockPercent = 0;

    bool GetTaskbarCursor(POINT& point)
    {
        if (!GetCursorPos(&point))
            return false;

        const auto taskbar = GetAncestor(WindowFromPoint(point), GA_ROOT);
        wchar_t className[32]{};
        if (!taskbar || !GetClassNameW(taskbar, className, ARRAYSIZE(className)) ||
            (wcscmp(className, L"Shell_TrayWnd") != 0 && wcscmp(className, L"Shell_SecondaryTrayWnd") != 0))
            return false;

        // The taskbar automation peers return bounds relative to the taskbar host.
        return ScreenToClient(taskbar, &point) != FALSE;
    }

    int ReadDockPercent()
    {
        const auto mapping = OpenFileMappingW(FILE_MAP_READ, FALSE, LR"(Local\WindowTab.Taskbar.DockScale.6)");
        if (!mapping)
            return 0;

        const auto view = static_cast<const volatile LONG*>(MapViewOfFile(mapping, FILE_MAP_READ, 0, 0, sizeof(LONG)));
        const LONG value = view ? *view : 0;
        if (view)
            UnmapViewOfFile(const_cast<const LONG*>(view));
        CloseHandle(mapping);
        return std::clamp<LONG>(value, 0, 200);
    }

    void CALLBACK AnimateDock(HWND, UINT, UINT_PTR, DWORD) noexcept
    {
        POINT cursor{};
        const bool enabled = dockPercent > 100 && !dockButtons.empty();
        const bool hovered = enabled && GetTaskbarCursor(cursor);
        bool restoring = false;

        for (auto& [handle, state] : dockButtons)
        {
            if (!hovered && state.current == 1)
                continue;

            try
            {
                const auto button = state.element.get();
                if (!button)
                    continue;

                double target = 1;
                if (hovered)
                {
                    // Compare the cursor and button in the taskbar host's coordinate space.
                    const auto peer = peers::FrameworkElementAutomationPeer::CreatePeerForElement(button);
                    if (peer)
                    {
                        const auto bounds = peer.GetBoundingRectangle();
                        if (bounds.Width > 0 && cursor.y >= bounds.Y && cursor.y < bounds.Y + bounds.Height)
                        {
                            const auto distance = std::abs(cursor.x - (bounds.X + bounds.Width / 2));
                            const auto baseWidth = bounds.Width / state.current;
                            const auto weight = std::clamp(1 - distance / (1.5 * baseWidth), 0.0, 1.0);
                            target += (dockPercent / 100.0 - 1) * weight * weight * (3 - 2 * weight);
                        }
                    }
                }
                state.Update(button, target);
                restoring |= state.current != 1;
            }
            catch (...) { }
        }

        if (!enabled && !restoring)
        {
            KillTimer(nullptr, dockTimer);
            dockTimer = 0;
        }
    }

    void CALLBACK Refresh(HWND, UINT, UINT_PTR, DWORD) noexcept
    {
        const auto request = OpenEventW(SYNCHRONIZE, FALSE, enabledEventName);
        const bool enabled = request && WaitForSingleObject(request, 0) == WAIT_OBJECT_0;
        if (request)
            CloseHandle(request);

        dockPercent = ReadDockPercent();
        if (dockPercent > 100 && !dockButtons.empty())
        {
            if (!dockTimer)
                dockTimer = SetTimer(nullptr, 0, 16, AnimateDock);
            if (const auto status = OpenEventW(EVENT_MODIFY_STATE, FALSE, LR"(Local\WindowTab.Taskbar.DockApplied.6)"))
            {
                SetEvent(status);
                CloseHandle(status);
            }
        }

        bool applied = false;
        for (auto& [handle, background] : backgrounds)
        {
            try
            {
                background.Update(enabled);
                applied |= background.hidden;
            }
            catch (...) { }
        }
        if (applied)
        {
            if (const auto status = OpenEventW(EVENT_MODIFY_STATE, FALSE, appliedEventName))
            {
                SetEvent(status);
                CloseHandle(status);
            }
        }
    }

    struct Watcher : winrt::implements<Watcher, IVisualTreeServiceCallback2>
    {
        explicit Watcher(winrt::com_ptr<IXamlDiagnostics> diagnostics) : diagnostics(std::move(diagnostics)) {}

        HRESULT STDMETHODCALLTYPE OnVisualTreeChange(ParentChildRelation, VisualElement element,
            VisualMutationType mutation) noexcept override
        {
            const auto isBackground = mutation == Add && element.Name &&
                (wcscmp(element.Name, L"BackgroundFill") == 0 || wcscmp(element.Name, L"BackgroundStroke") == 0);
            const auto isDockButton = mutation == Add && element.Type &&
                wcscmp(element.Type, L"Taskbar.TaskListButton") == 0;
            SysFreeString(element.Name);
            SysFreeString(element.Type);
            SysFreeString(element.SrcInfo.FileName);
            SysFreeString(element.SrcInfo.Hash);

            try
            {
                if (mutation == Remove)
                {
                    if (const auto it = dockButtons.find(element.Handle); it != dockButtons.end())
                    {
                        auto state = std::move(it->second);
                        dockButtons.erase(it);
                        if (const auto button = state.element.get())
                        {
                            state.current = 1;
                            state.Update(button, 1);
                        }
                    }
                    if (const auto it = backgrounds.find(element.Handle); it != backgrounds.end())
                    {
                        it->second.Update(false);
                        backgrounds.erase(it);
                    }
                    if (backgrounds.empty() && dockButtons.empty() && timerId)
                    {
                        KillTimer(nullptr, timerId);
                        timerId = 0;
                    }
                }
                else if (isDockButton)
                {
                    winrt::com_ptr<::IInspectable> object;
                    winrt::check_hresult(diagnostics->GetIInspectableFromHandle(element.Handle, object.put()));
                    if (const auto button = object.try_as<xaml::FrameworkElement>())
                    {
                        if (!timerId)
                            timerId = SetTimer(nullptr, 0, 300, Refresh);
                        if (timerId)
                            dockButtons.try_emplace(element.Handle, DockButton{ winrt::make_weak(button) });
                    }
                }
                else if (isBackground)
                {
                    winrt::com_ptr<::IInspectable> object;
                    winrt::check_hresult(diagnostics->GetIInspectableFromHandle(element.Handle, object.put()));
                    const auto shape = object.try_as<xaml::Shapes::Rectangle>();
                    if (!shape)
                        return S_OK;

                    auto parent = xaml::Media::VisualTreeHelper::GetParent(shape);
                    while (parent && winrt::get_class_name(parent) != L"Taskbar.TaskbarFrame")
                        parent = xaml::Media::VisualTreeHelper::GetParent(parent);

                    if (parent)
                    {
                        if (!timerId)
                            timerId = SetTimer(nullptr, 0, 300, Refresh);
                        if (timerId)
                        {
                            backgrounds.try_emplace(element.Handle, Background{ winrt::make_weak(shape) });
                            Refresh(nullptr, 0, 0, 0);
                        }
                    }
                }
            }
            catch (...) { }
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE OnElementStateChanged(InstanceHandle, VisualElementState, LPCWSTR) noexcept override
        {
            return S_OK;
        }

    private:
        winrt::com_ptr<IXamlDiagnostics> diagnostics;
    };

    struct Site : winrt::implements<Site, IObjectWithSite>
    {
        HRESULT STDMETHODCALLTYPE SetSite(IUnknown* value) noexcept override try
        {
            site.copy_from(value);
            if (site)
            {
                auto service = site.as<IVisualTreeService3>();
                auto watcher = winrt::make<Watcher>(site.as<IXamlDiagnostics>());
                // Keep the attachment marker for this Explorer process's lifetime.
                static const auto attachment = CreateEventW(nullptr, TRUE, TRUE, AttachmentName(GetCurrentProcessId()).c_str());
                if (!attachment)
                    return HRESULT_FROM_WIN32(GetLastError());
                static const auto version = CreateEventW(nullptr, TRUE, TRUE, (AttachmentName(GetCurrentProcessId()) + L".6").c_str());
                if (!version)
                    return HRESULT_FROM_WIN32(GetLastError());
                // Advising on the XAML thread itself can block while the initial tree is delivered.
                std::thread([service, watcher]
                {
                    if (SUCCEEDED(CoInitializeEx(nullptr, COINIT_MULTITHREADED)))
                    {
                        service->AdviseVisualTreeChange(watcher.get());
                        CoUninitialize();
                    }
                }).detach();
            }
            return S_OK;
        }
        catch (...) { return winrt::to_hresult(); }

        HRESULT STDMETHODCALLTYPE GetSite(REFIID iid, void** result) noexcept override
        {
            *result = nullptr;
            return site ? site->QueryInterface(iid, result) : E_FAIL;
        }

    private:
        winrt::com_ptr<IUnknown> site;
    };

    struct Factory : winrt::implements<Factory, IClassFactory>
    {
        HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept override try
        {
            *result = nullptr;
            return outer ? CLASS_E_NOAGGREGATION : winrt::make<Site>()->QueryInterface(iid, result);
        }
        catch (...) { return winrt::to_hresult(); }

        HRESULT STDMETHODCALLTYPE LockServer(BOOL) noexcept override { return S_OK; }
    };
}

extern "C" HRESULT WINAPI GetTaskbarClassObject(REFCLSID clsid, REFIID iid, void** result) try
{
    *result = nullptr;
    return clsid == siteClass ? winrt::make<Factory>()->QueryInterface(iid, result) : CLASS_E_CLASSNOTAVAILABLE;
}
catch (...) { return winrt::to_hresult(); }
