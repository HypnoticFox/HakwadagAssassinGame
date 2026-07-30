import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import Button from '@/components/Button.vue'

describe('Button.vue', () => {
  it('renders default slot content', () => {
    const wrapper = mount(Button, {
      slots: { default: 'Click me' },
    })
    expect(wrapper.text()).toContain('Click me')
  })

  describe('variants', () => {
    it('has primary class by default', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).toContain('button--primary')
    })

    it('applies secondary class', () => {
      const wrapper = mount(Button, {
        props: { variant: 'secondary' },
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).toContain('button--secondary')
    })

    it('applies danger class', () => {
      const wrapper = mount(Button, {
        props: { variant: 'danger' },
        slots: { default: 'Delete' },
      })
      expect(wrapper.classes()).toContain('button--danger')
    })

    it('applies ghost class', () => {
      const wrapper = mount(Button, {
        props: { variant: 'ghost' },
        slots: { default: 'Cancel' },
      })
      expect(wrapper.classes()).toContain('button--ghost')
    })
  })

  describe('sizes', () => {
    it('has default size class by default', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).not.toContain('button--large')
    })

    it('applies large class', () => {
      const wrapper = mount(Button, {
        props: { size: 'large' },
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).toContain('button--large')
    })
  })

  describe('loading state', () => {
    it('adds loading class when loading is true', () => {
      const wrapper = mount(Button, {
        props: { loading: true },
        slots: { default: 'Saving' },
      })
      expect(wrapper.classes()).toContain('button--loading')
    })

    it('disables button when loading', () => {
      const wrapper = mount(Button, {
        props: { loading: true },
        slots: { default: 'Saving' },
      })
      const button = wrapper.find('button')
      expect(button.attributes('disabled')).toBeDefined()
    })

    it('does not have loading class when loading is false', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).not.toContain('button--loading')
    })
  })

  describe('full width', () => {
    it('applies full-width class', () => {
      const wrapper = mount(Button, {
        props: { fullWidth: true },
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).toContain('button--full-width')
    })

    it('does not have full-width class by default', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      expect(wrapper.classes()).not.toContain('button--full-width')
    })
  })

  describe('disabled state', () => {
    it('disables button when disabled prop is true', () => {
      const wrapper = mount(Button, {
        props: { disabled: true },
        slots: { default: 'Go' },
      })
      const button = wrapper.find('button')
      expect(button.attributes('disabled')).toBeDefined()
    })

    it('does not disable button by default', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      const button = wrapper.find('button')
      expect(button.attributes('disabled')).toBeUndefined()
    })
  })

  describe('click event', () => {
    it('emits click event when clicked', async () => {
      const wrapper = mount(Button, {
        slots: { default: 'Click' },
      })
      await wrapper.find('button').trigger('click')
      expect(wrapper.emitted('click')).toHaveLength(1)
    })

    it('passes the mouse event with click emit', async () => {
      const wrapper = mount(Button, {
        slots: { default: 'Click' },
      })
      await wrapper.find('button').trigger('click')
      const emitted = wrapper.emitted('click')![0]
      expect(emitted[0]).toBeInstanceOf(MouseEvent)
    })

    it('does not emit click when disabled', async () => {
      const wrapper = mount(Button, {
        props: { disabled: true },
        slots: { default: 'Click' },
      })
      await wrapper.find('button').trigger('click')
      expect(wrapper.emitted('click')).toBeUndefined()
    })

    it('does not emit click when loading', async () => {
      const wrapper = mount(Button, {
        props: { loading: true },
        slots: { default: 'Saving' },
      })
      await wrapper.find('button').trigger('click')
      expect(wrapper.emitted('click')).toBeUndefined()
    })
  })

  describe('type attribute', () => {
    it('defaults to type button', () => {
      const wrapper = mount(Button, {
        slots: { default: 'Go' },
      })
      expect(wrapper.find('button').attributes('type')).toBe('button')
    })

    it('sets type submit', () => {
      const wrapper = mount(Button, {
        props: { type: 'submit' },
        slots: { default: 'Submit' },
      })
      expect(wrapper.find('button').attributes('type')).toBe('submit')
    })
  })
})
