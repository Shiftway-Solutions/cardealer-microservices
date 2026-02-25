'use client';

import * as React from 'react';
import { ChevronDown, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';

export interface SearchableSelectProps {
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
  placeholder?: string;
  searchPlaceholder?: string;
  isLoading?: boolean;
  disabled?: boolean;
  className?: string;
  clearable?: boolean;
}

/**
 * SearchableSelect - A combobox component that allows typing to filter options
 * Perfect for vehicle properties (make, model, body type, fuel, etc.)
 */
export const SearchableSelect = React.forwardRef<HTMLButtonElement, SearchableSelectProps>(
  (
    {
      value,
      onChange,
      options,
      placeholder = 'Seleccionar...',
      searchPlaceholder = 'Buscar...',
      isLoading = false,
      disabled = false,
      className = '',
      clearable = true,
    },
    ref
  ) => {
    const [open, setOpen] = React.useState(false);
    const [searchValue, setSearchValue] = React.useState('');
    const inputRef = React.useRef<HTMLInputElement>(null);

    // Filter options based on search
    const filteredOptions = React.useMemo(() => {
      if (!searchValue) return options;
      const search = searchValue.toLowerCase();
      return options.filter(
        opt => opt.label.toLowerCase().includes(search) || opt.value.toLowerCase().includes(search)
      );
    }, [searchValue, options]);

    // Get selected label
    const selectedLabel = React.useMemo(
      () => options.find(opt => opt.value === value)?.label || '',
      [options, value]
    );

    // Focus search input when popover opens
    React.useEffect(() => {
      if (open) {
        setTimeout(() => inputRef.current?.focus(), 0);
      } else {
        setSearchValue('');
      }
    }, [open]);

    // Handle keyboard navigation
    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Escape') {
        setOpen(false);
      }
    };

    return (
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            ref={ref}
            variant="outline"
            role="combobox"
            aria-expanded={open}
            disabled={disabled || isLoading}
            className={`w-full justify-between ${className} ${
              disabled ? 'cursor-not-allowed opacity-60' : ''
            }`}
          >
            <span className={selectedLabel ? 'text-foreground' : 'text-muted-foreground'}>
              {isLoading ? 'Cargando...' : selectedLabel || placeholder}
            </span>
            <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
          <div className="flex flex-col">
            {/* Search input */}
            <div className="border-b p-2">
              <Input
                ref={inputRef}
                placeholder={searchPlaceholder}
                value={searchValue}
                onChange={e => setSearchValue(e.target.value)}
                onKeyDown={handleKeyDown}
                className="h-8"
              />
            </div>

            {/* Options list */}
            <div className="max-h-[200px] overflow-y-auto">
              {filteredOptions.length === 0 ? (
                <div className="text-muted-foreground py-4 text-center text-sm">
                  No hay opciones
                </div>
              ) : (
                filteredOptions.map(option => (
                  <button
                    key={option.value}
                    onClick={() => {
                      onChange(option.value);
                      setOpen(false);
                    }}
                    className={`hover:bg-accent w-full px-3 py-2 text-left text-sm ${
                      value === option.value ? 'bg-accent font-medium' : ''
                    }`}
                  >
                    {option.label}
                  </button>
                ))
              )}
            </div>

            {/* Clear button */}
            {clearable && value && (
              <div className="border-t p-2">
                <Button
                  variant="ghost"
                  size="sm"
                  className="w-full justify-start text-xs"
                  onClick={() => {
                    onChange('');
                    setOpen(false);
                  }}
                >
                  <X className="mr-1 h-3 w-3" />
                  Limpiar
                </Button>
              </div>
            )}
          </div>
        </PopoverContent>
      </Popover>
    );
  }
);

SearchableSelect.displayName = 'SearchableSelect';
