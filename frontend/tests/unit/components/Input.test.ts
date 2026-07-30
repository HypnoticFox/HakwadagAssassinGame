import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import Input from '@/components/Input.vue'

describe('Input.vue', () => {
  it('renders an input element', () => {
    const wrapper = mount(Input, {
      props: { modelValue: '' },
    })
    expect(wrapper.find('input').exists()).toBe(true)
  })

  describe('v-model', () => {
    it('renders with initial modelValue', () => {
      const wrapper = mount(Input, {
        props: { modelValue: 'Hello' },
      })
      const input = wrapper.find('input')
      expect(input.element.value).toBe('Hello')
    })

    it('emits update:modelValue when typing', async () => {
      const wrapper = mount(Input, {
        props: { modelValue: '' },
      })
      const input = wrapper.find('input')
      await input.setValue('New value')
      expect(wrapper.emitted('update:modelValue')).toHaveLength(1)
      expect(wrapper.emitted('update:modelValue')![0]).toEqual(['New value'])
    })

    it('updates displayed value when modelValue prop changes', async () => {
      const wrapper = mount(Input, {
        props: { modelValue: 'Initial' },
      })
      await wrapper.setProps({ modelValue: 'Updated' })
      const input = wrapper.find('input')
      expect(input.element.value).toBe('Updated')
    })
  })

  describe('label', () => {
    it('renders label when provided', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', label: 'Email' },
      })
      expect(wrapper.find('.input-label').exists()).toBe(true)
      expect(wrapper.find('.input-label').text()).toBe('Email')
    })

    it('does not render label when not provided', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '' },
      })
      expect(wrapper.find('.input-label').exists()).toBe(false)
    })

    it('shows required asterisk when required is true', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', label: 'Email', required: true },
      })
      expect(wrapper.find('.input-required').exists()).toBe(true)
    })

    it('does not show required asterisk when required is false', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', label: 'Email' },
      })
      expect(wrapper.find('.input-required').exists()).toBe(false)
    })
  })

  describe('error state', () => {
    it('shows error message when error is provided', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', error: 'This field is required' },
      })
      expect(wrapper.find('.input-error').exists()).toBe(true)
      expect(wrapper.find('.input-error').text()).toBe('This field is required')
    })

    it('does not show error when error is null', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '' },
      })
      expect(wrapper.find('.input-error').exists()).toBe(false)
    })

    it('adds error class to input when error is present', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', error: 'Error!' },
      })
      expect(wrapper.find('input').classes()).toContain('input--error')
    })

    it('sets role="alert" on error paragraph', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', error: 'Error!' },
      })
      expect(wrapper.find('.input-error').attributes('role')).toBe('alert')
    })
  })

  describe('attributes passthrough', () => {
    it('passes type attribute to input', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', type: 'email' },
      })
      expect(wrapper.find('input').attributes('type')).toBe('email')
    })

    it('defaults type to text', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '' },
      })
      expect(wrapper.find('input').attributes('type')).toBe('text')
    })

    it('passes placeholder attribute', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', placeholder: 'Enter value' },
      })
      expect(wrapper.find('input').attributes('placeholder')).toBe('Enter value')
    })

    it('passes autocomplete attribute', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', autocomplete: 'email' },
      })
      expect(wrapper.find('input').attributes('autocomplete')).toBe('email')
    })

    it('passes inputmode attribute', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', inputmode: 'numeric' },
      })
      expect(wrapper.find('input').attributes('inputmode')).toBe('numeric')
    })

    it('sets required attribute when required is true', () => {
      const wrapper = mount(Input, {
        props: { modelValue: '', required: true },
      })
      expect(wrapper.find('input').attributes('required')).toBeDefined()
    })
  })
})
