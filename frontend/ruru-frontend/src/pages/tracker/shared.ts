/** Shows a toast, with an optional Undo action. Provided by the Tracker page. */
export type Notify = (msg: string, undo?: () => void) => void

export interface TabProps {
  /** Only the active tab is shown; the other stays mounted so nothing typed is lost. */
  active: boolean
  notify: Notify
}
