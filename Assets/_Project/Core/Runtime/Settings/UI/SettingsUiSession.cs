using Project.Core.Input;
using System;
using System.Collections.Generic;

namespace Project.Core.Settings.Ui
{
    public sealed class SettingsUiSession
    {
        private sealed class FolderFrame
        {
            public SettingsFolder Folder;
            public int IndexInParent;
        }

        private readonly ISettingsRootBuilder _rootBuilder;

        private readonly Stack<FolderFrame> _stack = new();

        private List<SettingsItem> _items = new();
        private int _index;

        private SettingsUiMode _mode = SettingsUiMode.Browse;

        private SettingsList _editingList;
        private IReadOnlyList<SettingsListOption> _listOptions;
        private int _listIndex;

        private SettingsRange _editingRange;
        private float _rangeValue;

        private SettingsAction _pendingAction;

        public SettingsUiMode Mode => _mode;

        public IReadOnlyList<SettingsItem> Items => _items;
        public int Index => _index;
        public SettingsItem CurrentItem => (_items != null && _items.Count > 0 && _index >= 0 && _index < _items.Count) ? _items[_index] : null;

        public SettingsList EditingList => _editingList;
        public IReadOnlyList<SettingsListOption> ListOptions => _listOptions;
        public int ListIndex => _listIndex;

        public SettingsRange EditingRange => _editingRange;
        public float RangeValue => _rangeValue;

        public SettingsAction PendingAction => _pendingAction;

        public int FolderDepth => _stack.Count;

        public SettingsUiSession(ISettingsRootBuilder rootBuilder)
        {
            _rootBuilder = rootBuilder ?? throw new ArgumentNullException(nameof(rootBuilder));
        }

        public void Enter()
        {
            BuildRoot();
            _index = ClampIndex(_index, _items);
            _mode = SettingsUiMode.Browse;

            ClearEditState();
            _pendingAction = null;
        }

        public SettingsUiResult Handle(NavAction action, ISettingsUiHooks hooks)
        {
            hooks ??= NullHooks.Instance;

            switch (_mode)
            {
                case SettingsUiMode.Browse:
                    return HandleBrowse(action, hooks);

                case SettingsUiMode.EditList:
                    return HandleEditList(action);

                case SettingsUiMode.EditRange:
                    return HandleEditRange(action);

                case SettingsUiMode.ConfirmAction:
                    return HandleConfirmAction(action);

                default:
                    return SettingsUiResult.None;
            }
        }

        private SettingsUiResult HandleBrowse(NavAction action, ISettingsUiHooks hooks)
        {
            if (_items == null || _items.Count == 0)
                return SettingsUiResult.None;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % _items.Count;
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Previous:
                    _index = (_index - 1 + _items.Count) % _items.Count;
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Confirm:
                    return ConfirmCurrent(hooks);

                case NavAction.Back:
                    return BackFromBrowse();

                default:
                    return SettingsUiResult.None;
            }
        }

        private SettingsUiResult ConfirmCurrent(ISettingsUiHooks hooks)
        {
            var item = CurrentItem;
            if (item == null)
                return SettingsUiResult.None;

            if (IsBackItem(item))
            {
                var r = BackFromBrowse();
                return new SettingsUiResult(
                    selectionChanged: r.SelectionChanged,
                    modeChanged: r.ModeChanged,
                    enteredFolder: r.EnteredFolder,
                    exitedFolderOrRoot: r.ExitedFolderOrRoot,
                    startedEdit: r.StartedEdit,
                    cancelledEdit: r.CancelledEdit,
                    committedEdit: r.CommittedEdit,
                    startedConfirmAction: r.StartedConfirmAction,
                    cancelledConfirmAction: r.CancelledConfirmAction,
                    confirmedAction: r.ConfirmedAction,
                    confirmedBackItem: true,
                    backFromRoot: r.BackFromRoot
                );
            }

            switch (item.Type)
            {
                case SettingsItemType.Folder:
                    return EnterFolder((SettingsFolder)item);

                case SettingsItemType.List:
                    return BeginEditList((SettingsList)item);

                case SettingsItemType.Range:
                    return BeginEditRange((SettingsRange)item);

                case SettingsItemType.Toggle:
                    {
                        var t = (SettingsToggle)item;

                        if (hooks.TryHandleToggle(t))
                            return new SettingsUiResult(
                                handledByHooks: true,
                                affectedItem: t,
                                affectedItemType: SettingsItemType.Toggle
                            );

                        bool now = !t.GetValue();
                        t.SetValue(now);

                        return new SettingsUiResult(
                            affectedItem: t,
                            affectedItemType: SettingsItemType.Toggle,
                            valueChanged: true,
                            toggleValue: now
                        );
                    }

                case SettingsItemType.Action:
                    {
                        var a = (SettingsAction)item;

                        if (hooks.TryHandleAction(a))
                            return new SettingsUiResult(
                                handledByHooks: true,
                                affectedItem: a,
                                affectedItemType: SettingsItemType.Action
                            );

                        if (hooks.RequiresConfirmation(a))
                        {
                            _pendingAction = a;
                            var prev = _mode;
                            _mode = SettingsUiMode.ConfirmAction;

                            return new SettingsUiResult(
                                modeChanged: prev != _mode,
                                startedConfirmAction: true,
                                affectedItem: a,
                                affectedItemType: SettingsItemType.Action
                            );
                        }

                        a.Execute?.Invoke();

                        return new SettingsUiResult(
                            affectedItem: a,
                            affectedItemType: SettingsItemType.Action,
                            actionExecuted: true
                        );
                    }

                default:
                    return SettingsUiResult.None;
            }
        }

        private SettingsUiResult BackFromBrowse()
        {
            if (_stack.Count > 0)
            {
                var frame = _stack.Pop();

                if (_stack.Count == 0)
                {
                    BuildRoot();
                }
                else
                {
                    var parent = _stack.Peek().Folder;
                    _items = parent.BuildChildren?.Invoke() ?? new List<SettingsItem>();
                    EnsureBackAtEnd();
                }

                _index = ClampIndex(frame.IndexInParent, _items);
                _mode = SettingsUiMode.Browse;

                ClearEditState();
                _pendingAction = null;

                return new SettingsUiResult(exitedFolderOrRoot: true, selectionChanged: true);
            }

            ClearEditState();
            _pendingAction = null;
            _mode = SettingsUiMode.Browse;
            return new SettingsUiResult(backFromRoot: true);
        }

        private SettingsUiResult EnterFolder(SettingsFolder folder)
        {
            if (folder == null)
                return SettingsUiResult.None;

            _stack.Push(new FolderFrame { Folder = folder, IndexInParent = _index });

            _items = folder.BuildChildren?.Invoke() ?? new List<SettingsItem>();
            EnsureBackAtEnd();

            _index = ClampIndex(0, _items);
            _mode = SettingsUiMode.Browse;

            ClearEditState();
            _pendingAction = null;

            return new SettingsUiResult(
                enteredFolder: true,
                selectionChanged: true,
                affectedItem: folder,
                affectedItemType: SettingsItemType.Folder
            );
        }

        private SettingsUiResult BeginEditList(SettingsList list)
        {
            if (list == null)
                return SettingsUiResult.None;

            _editingList = list;
            _listOptions = list.GetOptions?.Invoke();
            _listIndex = Math.Max(0, list.GetIndex());

            _editingRange = null;

            var prev = _mode;
            _mode = SettingsUiMode.EditList;

            return new SettingsUiResult(
                modeChanged: prev != _mode,
                startedEdit: true,
                affectedItem: list,
                affectedItemType: SettingsItemType.List
            );
        }

        private SettingsUiResult HandleEditList(NavAction action)
        {
            if (_editingList == null || _listOptions == null || _listOptions.Count == 0)
            {
                var prev = _mode;
                _mode = SettingsUiMode.Browse;
                ClearEditState();

                return new SettingsUiResult(modeChanged: prev != _mode, cancelledEdit: true);
            }

            switch (action)
            {
                case NavAction.Next:
                    _listIndex = (_listIndex + 1) % _listOptions.Count;
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Previous:
                    _listIndex = (_listIndex - 1 + _listOptions.Count) % _listOptions.Count;
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Confirm:
                    _editingList.SetIndex?.Invoke(_listIndex);

                    {
                        var committedItem = _editingList;

                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        ClearEditState();

                        return new SettingsUiResult(
                            modeChanged: prev != _mode,
                            committedEdit: true,
                            affectedItem: committedItem,
                            affectedItemType: SettingsItemType.List,
                            valueChanged: true,
                            listIndex: _listIndex
                        );
                    }

                case NavAction.Back:
                    {
                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        ClearEditState();
                        return new SettingsUiResult(modeChanged: prev != _mode, cancelledEdit: true);
                    }

                default:
                    return SettingsUiResult.None;
            }
        }

        private SettingsUiResult BeginEditRange(SettingsRange range)
        {
            if (range == null)
                return SettingsUiResult.None;

            _editingRange = range;

            float min = range.Min;
            float max = range.Max;
            float v = range.GetValue != null ? range.GetValue() : 0f;
            _rangeValue = Clamp(v, min, max);

            _editingList = null;
            _listOptions = null;
            _listIndex = 0;

            var prev = _mode;
            _mode = SettingsUiMode.EditRange;

            return new SettingsUiResult(
                modeChanged: prev != _mode,
                startedEdit: true,
                affectedItem: range,
                affectedItemType: SettingsItemType.Range
            );
        }

        private SettingsUiResult HandleEditRange(NavAction action)
        {
            if (_editingRange == null)
            {
                var prev = _mode;
                _mode = SettingsUiMode.Browse;
                ClearEditState();

                return new SettingsUiResult(modeChanged: prev != _mode, cancelledEdit: true);
            }

            float min = _editingRange.Min;
            float max = _editingRange.Max;

            switch (action)
            {
                case NavAction.Next:
                    _rangeValue = Clamp(_rangeValue + _editingRange.Step, min, max);
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Previous:
                    _rangeValue = Clamp(_rangeValue - _editingRange.Step, min, max);
                    return new SettingsUiResult(selectionChanged: true);

                case NavAction.Confirm:
                    _editingRange.SetValue?.Invoke(_rangeValue);

                    {
                        var committedItem = _editingRange;
                        var committedValue = _rangeValue;

                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        ClearEditState();

                        return new SettingsUiResult(
                            modeChanged: prev != _mode,
                            committedEdit: true,
                            affectedItem: committedItem,
                            affectedItemType: SettingsItemType.Range,
                            valueChanged: true,
                            rangeValue: committedValue
                        );
                    }

                case NavAction.Back:
                    {
                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        ClearEditState();
                        return new SettingsUiResult(modeChanged: prev != _mode, cancelledEdit: true);
                    }

                default:
                    return SettingsUiResult.None;
            }
        }

        private SettingsUiResult HandleConfirmAction(NavAction action)
        {
            switch (action)
            {
                case NavAction.Confirm:
                    var executed = _pendingAction;

                    _pendingAction?.Execute?.Invoke();
                    _pendingAction = null;

                    BuildRoot();
                    _index = ClampIndex(0, _items);

                    {
                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        return new SettingsUiResult(
                            modeChanged: prev != _mode,
                            confirmedAction: true,
                            affectedItem: executed,
                            affectedItemType: SettingsItemType.Action,
                            actionExecuted: true
                        );
                    }

                case NavAction.Back:
                    var cancelled = _pendingAction;
                    _pendingAction = null;

                    {
                        var prev = _mode;
                        _mode = SettingsUiMode.Browse;
                        return new SettingsUiResult(
                            modeChanged: prev != _mode,
                            cancelledConfirmAction: true,
                            affectedItem: cancelled,
                            affectedItemType: SettingsItemType.Action
                        );
                    }

                default:
                    return SettingsUiResult.None;
            }
        }

        private void BuildRoot()
        {
            _stack.Clear();
            _items = _rootBuilder.BuildRoot() ?? new List<SettingsItem>();
            EnsureBackAtEnd();
        }

        private void EnsureBackAtEnd()
        {
            if (_items == null || _items.Count == 0) return;

            int idx = _items.FindIndex(IsBackItem);
            if (idx >= 0 && idx != _items.Count - 1)
            {
                var back = _items[idx];
                _items.RemoveAt(idx);
                _items.Add(back);
            }
        }

        private void ClearEditState()
        {
            _editingList = null;
            _listOptions = null;
            _listIndex = 0;

            _editingRange = null;
            _rangeValue = 0f;
        }

        private static bool IsBackItem(SettingsItem it) => it != null && it.LabelKey == "common.back";

        private static int ClampIndex(int index, List<SettingsItem> items)
        {
            if (items == null || items.Count == 0) return 0;
            if (index < 0) return 0;
            if (index >= items.Count) return items.Count - 1;
            return index;
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        private sealed class NullHooks : ISettingsUiHooks
        {
            public static readonly NullHooks Instance = new NullHooks();

            public bool RequiresConfirmation(SettingsAction action) => false;
            public bool TryHandleToggle(SettingsToggle toggle) => false;
            public bool TryHandleAction(SettingsAction action) => false;
        }
    }
}