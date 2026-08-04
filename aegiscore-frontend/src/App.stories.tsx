import type { Meta, StoryFn, StoryObj } from "@storybook/react";
import { Provider } from "react-redux";
import { App } from "./App";
import { store } from "./store";

const meta = {
  title: "Portals/App",
  component: App,
  decorators: [(Story: StoryFn) => <Provider store={store}><Story /></Provider>]
} satisfies Meta<typeof App>;

export default meta;
export const Default: StoryObj<typeof meta> = {};
